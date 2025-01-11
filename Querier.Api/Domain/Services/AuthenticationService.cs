using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Tools;

namespace Querier.Api.Domain.Services
{
    public class AuthenticationService(
        IUserRepository userRepository,
        ISettingService settingService,
        IAuthenticationRepository authenticationRepository,
        TokenValidationParameters tokenValidationParameters,
        ILogger<AuthenticationService> logger)
        : IAuthenticationService
    {
        private async Task<AuthResultDto> GenerateJwtToken(ApiUser user)
        {
            try
        {
            var jwtSecret = await settingService.GetSettingValueAsync<string>("jwt:secret");
            var jwtIssuer = await settingService.GetSettingValueAsync<string>("jwt:issuer");
            var jwtAudience = await settingService.GetSettingValueAsync<string>("jwt:audience");
            var jwtExpiryInMinutes = await settingService.GetSettingValueAsync<int>("jwt:expiry");
                var refreshTokenExpiryInDays = await settingService.GetSettingValueAsync<int>("jwt:refreshTokenExpiry");
                if (refreshTokenExpiryInDays == 0) refreshTokenExpiryInDays = 180; // 6 mois par défaut

            if (string.IsNullOrEmpty(jwtSecret))
                throw new InvalidOperationException("JWT secret is not configured");

                if (user.Email == null)
                    throw new InvalidOperationException("User email cannot be null");

                logger.LogInformation("Generating JWT token for user: {Email}", user.Email);

            var userRoles = await userRepository.GetRolesAsync(user);
                var claims = new List<Claim>
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                foreach (var role in userRoles.Where(r => r.Name != null))
                {
                    if (role.Name != null) 
                        claims.Add(new Claim(ClaimTypes.Role, role.Name));
                }

                var key = Encoding.ASCII.GetBytes(jwtSecret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(jwtExpiryInMinutes),
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
                    ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryInDays),
                    IsRevoked = false,
                    Token = Utils.RandomString(25) + Guid.NewGuid()
                };

                await authenticationRepository.AddRefreshTokenAsync(refreshToken);
                logger.LogInformation("Successfully generated JWT token for user: {Email}", user.Email);

                return new AuthResultDto()
                {
                    Token = jwtToken,
                    Success = true,
                    RefreshToken = refreshToken.Token
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating JWT token for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<SignUpResultDto> SignUp(SignUpDto user)
        {
            try
            {
                logger.LogInformation("Attempting to sign up user: {Email}", user.Email);

                var existingUser = await userRepository.GetByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    logger.LogWarning("Sign up failed - Email already exists: {Email}", user.Email);
                    return new SignUpResultDto()
                    {
                        Success = false,
                        Errors = ["Email already exists"]
                    };
                }

                var newUser = new ApiUser() 
                { 
                    Email = user.Email, 
                    UserName = user.Email, 
                    FirstName = user.FirstName, 
                    LastName = user.LastName 
                };

            var isCreated = await userRepository.AddAsync(newUser);
            if (!isCreated.Succeeded)
                {
                    logger.LogWarning("Failed to create user: {Email}. Errors: {@Errors}", 
                        user.Email, isCreated.Errors);
                return new SignUpResultDto()
                {
                    Success = false,
                    Errors = isCreated.Errors.Select(x => x.Description).ToList()
                };
                }
            
            var authResult = await GenerateJwtToken(newUser);
                logger.LogInformation("Successfully signed up user: {Email}", user.Email);

            return new SignUpResultDto()
            {
                Success = true,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RefreshToken = authResult.RefreshToken,
                Token = authResult.Token,
                UserName = user.UserName,
                    Roles = user.Roles
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during sign up for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<SignUpResultDto> SignIn(SignInDto user)
        {
            try
            {
                logger.LogInformation("Attempting to sign in user: {Email}", user.Email);

            var existingUser = await userRepository.GetByEmailAsync(user.Email);
            if (existingUser == null)
            {
                    logger.LogWarning("Sign in failed - User not found: {Email}", user.Email);
                return new SignUpResultDto()
                {
                    Success = false,
                        Errors = ["Invalid credentials"]
                };
            }

            bool passwordIsCorrect = await userRepository.CheckPasswordAsync(existingUser, user.Password);
            bool emailConfirmed = await userRepository.IsEmailConfirmedAsync(existingUser);

                if (!emailConfirmed)
                {
                    logger.LogWarning("Sign in failed - Email not confirmed: {Email}", user.Email);
                    return new SignUpResultDto()
                    {
                        Success = false,
                        Errors = ["Email not confirmed"]
                    };
                }

                if (!passwordIsCorrect)
                {
                    logger.LogWarning("Sign in failed - Invalid password: {Email}", user.Email);
                    return new SignUpResultDto()
                    {
                        Success = false,
                        Errors = ["Invalid credentials"]
                    };
                }

                var authResult = await GenerateJwtToken(existingUser);
                var roles = await userRepository.GetRolesAsync(existingUser);

                logger.LogInformation("Successfully signed in user: {Email}", user.Email);

                return new SignUpResultDto()
                {
                    Id = existingUser.Id,
                    FirstName = existingUser.FirstName,
                    LastName = existingUser.LastName,
                    Roles = roles.Select(role => role.Name).ToList(),
                    RefreshToken = authResult.RefreshToken,
                    Success = true,
                    Token = authResult.Token,
                    Email = existingUser.Email,
                    UserName = existingUser.UserName
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during sign in for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<AuthResultDto> RefreshToken(TokenRequest tokenRequest)
        {
            try
            {
                logger.LogInformation("Attempting to refresh token");

                var jwtSecret = await settingService.GetSettingValueAsync<string>("jwt:secret");
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    logger.LogError("JWT secret is not configured");
                    throw new InvalidOperationException("JWT secret is not configured");
                }

                var key = Encoding.ASCII.GetBytes(jwtSecret);
                var signingKey = new SymmetricSecurityKey(key);

                tokenValidationParameters.ValidateLifetime = false;
                tokenValidationParameters.IssuerSigningKey = signingKey;

                var jwtTokenHandler = new JwtSecurityTokenHandler();
                ClaimsPrincipal principal;

                try
                {
                    principal = jwtTokenHandler.ValidateToken(tokenRequest.Token, tokenValidationParameters, out var validatedToken);

                    if (validatedToken is not JwtSecurityToken jwtSecurityToken || 
                        !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    {
                        logger.LogWarning("Invalid token algorithm");
                        return new AuthResultDto
                        {
                            Success = false,
                            Errors = ["Invalid token"]
                        };
                    }
                }
                catch (SecurityTokenException ex)
                {
                    logger.LogWarning(ex, "Token validation failed");
                    return new AuthResultDto
                    {
                        Success = false,
                        Errors = ["Invalid token"]
                    };
                }

                var expiryDateUnix = long.Parse(principal.Claims.First(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expiryDateUtc = Utils.UnixTimeStampToDateTime(expiryDateUnix);

                if (expiryDateUtc > DateTime.UtcNow)
                {
                    logger.LogWarning("Token has not expired yet");
                    return new AuthResultDto
                    {
                        Success = false,
                        Errors =
                        [
                            $"Token is still valid for {(expiryDateUtc - DateTime.UtcNow).TotalMinutes:F1} minutes"
                        ]
                    };
                }

                var storedRefreshToken = await authenticationRepository.GetRefreshTokenAsync(tokenRequest.RefreshToken);
                if (storedRefreshToken == null)
                {
                    logger.LogWarning("Refresh token not found");
                    return new AuthResultDto
                    {
                        Success = false,
                        Errors = ["Invalid refresh token"]
                    };
                }

                if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
                {
                    logger.LogWarning("Refresh token has expired");
                    return new AuthResultDto
                    {
                        Success = false,
                        Errors = ["Refresh token has expired"]
                    };
                }

                if (storedRefreshToken.IsUsed)
                {
                    logger.LogWarning("Refresh token has already been used");
                    return new AuthResultDto
                    {
                        Success = false,
                        Errors = ["Refresh token has already been used"]
                    };
                }

                if (storedRefreshToken.IsRevoked)
                {
                    logger.LogWarning("Refresh token has been revoked");
                    return new AuthResultDto
                    {
                        Success = false,
                        Errors = ["Refresh token has been revoked"]
                    };
                }

                var jti = principal.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
                if (storedRefreshToken.JwtId != jti)
                {
                    logger.LogWarning("Refresh token does not match JWT");
                    return new AuthResultDto
                    {
                        Success = false,
                        Errors = ["Invalid refresh token"]
                    };
                }

                storedRefreshToken.IsUsed = true;
                await authenticationRepository.UpdateRefreshTokenAsync(storedRefreshToken);

                var user = await userRepository.GetByIdAsync(storedRefreshToken.UserId);
                if (user == null)
                {
                    logger.LogError("User not found for refresh token");
                    return new AuthResultDto
                    {
                        Success = false,
                        Errors = ["User not found"]
                    };
                }

                logger.LogInformation("Successfully refreshed token for user: {Email}", user.Email);
                return await GenerateJwtToken(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during token refresh");
                throw;
            }
        }
    }
}
