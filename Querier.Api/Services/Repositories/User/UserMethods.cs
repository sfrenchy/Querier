using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Tools;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static Google.Apis.Auth.JsonWebToken;

namespace Querier.Api.Services.Repositories.User
{
    public static class UserMethods
    {
        //Code mort//
        public static async Task<RegistrationResponse> Register([FromBody] UserRegistrationRequest user, UserManager<ApiUser> userManager, JwtConfig jwtConfig, ApiDbContext apiDbContext)
        {
            // check if the user with the same email exist
            var existingUser = await userManager.FindByEmailAsync(user.Email);

            if (existingUser != null)
            {
                return new RegistrationResponse()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Email already exist"
                    }
                };
            }

            var newUser = new ApiUser() { Email = user.Email, UserName = user.Email, FirstName = user.FirstName, LastName = user.LastName };
            var isCreated = await userManager.CreateAsync(newUser, user.Password);

            if (isCreated.Succeeded)
            {
                var result = await GenerateJwtToken(newUser, jwtConfig, apiDbContext);
                var userObj = await userManager.FindByEmailAsync(user.Email);

                return new RegistrationResponse()
                {
                    Success = true,
                    Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RefreshToken = result.RefreshToken,
                    Token = result.Token,
                    UserName = user.UserName,
                    Roles = user.Roles,
                };
            }

            return new RegistrationResponse()
            {
                Success = false,
                Errors = isCreated.Errors.Select(x => x.Description).ToList()
            };
        }
        //  //

        public static async Task<Payload> VerifyGoogleToken(string idToken)
        {
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(idToken);
            return payload;
        }

        public static async Task<AuthResult> VerifyToken(TokenRequest tokenRequest, TokenValidationParameters tokenValidationParameters, UserManager<ApiUser> userManager, JwtConfig jwtConfig, ApiDbContext apiDbContext)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // This validation function will make sure that the token meets the validation parameters
                // and its an actual jwt token not just a random string
                tokenValidationParameters.ValidateLifetime = false;
                var principal = jwtTokenHandler.ValidateToken(tokenRequest.Token, tokenValidationParameters, out var validatedToken);

                // Now we need to check if the token has a valid security algorithm
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);

                    if (result == false)
                    {
                        return null;
                    }
                }

                // Will get the time stamp in unix time
                var utcExpiryDate = long.Parse(principal.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);

                // we convert the expiry date from seconds to the date
                var expDate = UnixTimeStampToDateTime(utcExpiryDate);

                if (expDate > DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>() { "We cannot refresh this since the token has not expired" },
                        Success = false
                    };
                }

                // Check the token we got if its saved in the db
                var storedRefreshToken = await apiDbContext.QRefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

                if (storedRefreshToken == null)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>() { "refresh token doesnt exist" },
                        Success = false
                    };
                }

                // Check the date of the saved token if it has expired
                if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>() { "token has expired, user needs to relogin" },
                        Success = false
                    };
                }

                // check if the refresh token has been used
                if (storedRefreshToken.IsUsed)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>() { "token has been used" },
                        Success = false
                    };
                }

                // Check if the token is revoked
                if (storedRefreshToken.IsRevoked)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>() { "token has been revoked" },
                        Success = false
                    };
                }

                // we are getting here the jwt token id
                var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                // check the id that the recieved token has against the id saved in the db
                if (storedRefreshToken.JwtId != jti)
                {
                    return new AuthResult()
                    {
                        Errors = new List<string>() { "the token doenst mateched the saved token" },
                        Success = false
                    };
                }

                storedRefreshToken.IsUsed = true;
                apiDbContext.QRefreshTokens.Update(storedRefreshToken);
                await apiDbContext.SaveChangesAsync();

                var dbUser = await userManager.FindByIdAsync(storedRefreshToken.UserId);
                return await GenerateJwtToken(dbUser, jwtConfig, apiDbContext);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static async Task<AuthResult> GenerateJwtToken(ApiUser user, JwtConfig jwtConfig, ApiDbContext apiDbContext)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(jwtConfig.Secret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim("Id", user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        }),
                Expires = DateTime.UtcNow.Add(jwtConfig.ExpiryTimeFrame),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new QRefreshToken()
            {
                JwtId = token.Id,
                IsUsed = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsRevoked = false,
                Token = Utils.RandomString(25) + Guid.NewGuid()
            };

            await apiDbContext.QRefreshTokens.AddAsync(refreshToken);
            await apiDbContext.SaveChangesAsync();

            return new AuthResult()
            {
                Token = jwtToken,
                Success = true,
                RefreshToken = refreshToken.Token
            };
        }

        private static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }
    }
}
