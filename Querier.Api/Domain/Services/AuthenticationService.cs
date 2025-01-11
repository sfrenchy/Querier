using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.Data.Repositories;
using Querier.Api.Tools;

namespace Querier.Api.Domain.Services
{
    public class AuthenticationService(
        IUserRepository userRepository,
        ISettingService settingService,
        IAuthenticationRepository authenticationRepository,
        TokenValidationParameters tokenValidationParameters)
        : IAuthenticationService
    {
        private async Task<AuthResultDto> GenerateJwtToken(ApiUser user)
        {
            var jwtSecret = await settingService.GetSettingValueAsync<string>("jwt:secret");
            var jwtIssuer = await settingService.GetSettingValueAsync<string>("jwt:issuer");
            var jwtAudience = await settingService.GetSettingValueAsync<string>("jwt:audience");
            var jwtExpiryInMinutes = await settingService.GetSettingValueAsync<int>("jwt:expiry");

            if (string.IsNullOrEmpty(jwtSecret))
                throw new InvalidOperationException("JWT secret is not configured");

            // Récupérer les rôles de l'utilisateur
            var userRoles = await userRepository.GetRolesAsync(user);

            if (user.Email != null)
            {
                var claims = new List<Claim>
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Ajouter les rôles aux claims
                foreach (var role in userRoles)
                {
                    if (role.Name != null) 
                        claims.Add(new Claim(ClaimTypes.Role, role.Name));
                }

                var key = Encoding.ASCII.GetBytes(jwtSecret);
                var expiryInMinutes = jwtExpiryInMinutes;
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature
                    ),
                    Issuer = jwtIssuer,
                    Audience = jwtAudience
                };

                var jwtTokenHandler = new JwtSecurityTokenHandler();
                var token = jwtTokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = jwtTokenHandler.WriteToken(token);

                var refreshToken = new RefreshToken()
                {
                    JwtId = token.Id,
                    IsUsed = false,
                    UserId = user.Id,
                    AddedDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddMonths(6),
                    IsRevoked = false,
                    Token = Utils.RandomString(25) + Guid.NewGuid()
                };

                authenticationRepository.AddRefreshTokenAsync(refreshToken);

                return new AuthResultDto()
                {
                    Token = jwtToken,
                    Success = true,
                    RefreshToken = refreshToken.Token
                };
            }
            else
            {
                throw new InvalidOperationException("user email is null");
            }
        }
        public async Task<SignUpResultDto> SignUp(SignUpDto user)
        {
            // check if the user with the same email exist
            var existingUser = await userRepository.GetByEmailAsync(user.Email);

            if (existingUser != null)
            {
                return new SignUpResultDto()
                {
                    Success = false,
                    Errors = new List<string>()
                    {
                        "Email already exist"
                    }
                };
            }

            var newUser = new ApiUser() { Email = user.Email, UserName = user.Email, FirstName = user.FirstName, LastName = user.LastName };
            var isCreated = await userRepository.AddAsync(newUser);

            if (!isCreated.Succeeded)
                return new SignUpResultDto()
                {
                    Success = false,
                    Errors = isCreated.Errors.Select(x => x.Description).ToList()
                };
            
            var authResult = await GenerateJwtToken(newUser);

            return new SignUpResultDto()
            {
                Success = true,
                Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RefreshToken = authResult.RefreshToken,
                Token = authResult.Token,
                UserName = user.UserName,
                Roles = user.Roles,
            };
        }

        public async Task<SignUpResultDto> SignIn(SignInDto user)
        {
            var existingUser = await userRepository.GetByEmailAsync(user.Email);
            if (existingUser == null)
            {
                return new SignUpResultDto()
                {
                    Success = false,
                    Errors = new List<string>(){
                        "Invalid authentication request"
                    }
                };
            }

            bool passwordIsCorrect = await userRepository.CheckPasswordAsync(existingUser, user.Password);
            bool emailConfirmed = await userRepository.IsEmailConfirmedAsync(existingUser);

            if (passwordIsCorrect && emailConfirmed)
            {
                AuthResultDto r = await GenerateJwtToken(existingUser);
                var roles = await userRepository.GetRolesAsync(existingUser);

                return new SignUpResultDto()
                {
                    Id = existingUser.Id,
                    FirstName = existingUser.FirstName,
                    LastName = existingUser.LastName,
                    Roles = roles.Select(role => role.Name).ToList(),
                    RefreshToken = r.RefreshToken,
                    Success = r.Success,
                    Token = r.Token,
                    Email = existingUser.Email,
                    UserName = existingUser.UserName,
                };
            }
            else
            {
                return new SignUpResultDto()
                {
                    Success = false,
                    Errors = new List<string>(){
                        "Invalid authentication request"
                    }
                };
            }
        }

        public async Task<AuthResultDto> RefreshToken(TokenRequest tokenRequest)
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
                var expDate = Utils.UnixTimeStampToDateTime(utcExpiryDate);

                if (expDate > DateTime.UtcNow)
                {
                    return new AuthResultDto()
                    {
                        Errors = new List<string>() { "We cannot refresh this since the token has not expired" },
                        Success = false
                    };
                }

                // Check the token we got if its saved in the db
                var storedRefreshToken = await authenticationRepository.GetRefreshTokenAsync(tokenRequest.RefreshToken);
                if (storedRefreshToken == null)
                {
                    return new AuthResultDto()
                    {
                        Errors = new List<string>() { "refresh token doesnt exist" },
                        Success = false
                    };
                }

                // Check the date of the saved token if it has expired
                if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
                {
                    return new AuthResultDto()
                    {
                        Errors = new List<string>() { "token has expired, user needs to relogin" },
                        Success = false
                    };
                }

                // check if the refresh token has been used
                if (storedRefreshToken.IsUsed)
                {
                    return new AuthResultDto()
                    {
                        Errors = new List<string>() { "token has been used" },
                        Success = false
                    };
                }

                // Check if the token is revoked
                if (storedRefreshToken.IsRevoked)
                {
                    return new AuthResultDto()
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
                    return new AuthResultDto()
                    {
                        Errors = new List<string>() { "the token doesn't matched the saved token" },
                        Success = false
                    };
                }

                storedRefreshToken.IsUsed = true;
                authenticationRepository.UpdateRefreshTokenAsync(storedRefreshToken);

                var dbUser = await userRepository.GetByIdAsync(storedRefreshToken.UserId);
                return await GenerateJwtToken(dbUser);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
