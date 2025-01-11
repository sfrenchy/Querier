using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Data.Repositories;

namespace Querier.Api.Domain.Services
{
    public class UserService(
        ISettingService settings,
        ILogger<UserService> logger,
        IUserRepository userRepository,
        IEmailSendingService emailSending,
        IRoleRepository roleRepository)
        : IUserService
    {
        public async Task<bool> SetUserRolesAsync(string id, List<RoleDto> roles)
        {
            var foundUser = await userRepository.GetByIdAsync(id);
            if (foundUser == null)
            {
                logger.LogError($"User with id {id} does not exist");
                return false;
            }
            
            await userRepository.RemoveRolesAsync(foundUser);
            ApiRole[] apiRoles = roles.Select(RoleDto.ToEntity).ToArray();
            return await userRepository.AddRoleAsync(foundUser, apiRoles);
        }

        public async Task<bool> AddAsync(ApiUserCreateDto user)
        {
            var foundUser = await userRepository.GetByEmailAsync(user.Email);
            if (foundUser != null)
            {
                logger.LogError($"User with email {user.Email} already exists");
                return false;
            }
            ApiUser newUser = ApiUserDto.ToEntity(user);
            if (await userRepository.AddAsync(newUser) != IdentityResult.Success)
            {
                return false;
            }
            var roles = roleRepository.GetAll().Where(r => user.Roles.Contains(r.Name)).ToArray();
            return await userRepository.AddRoleAsync(newUser, roles);
        }

        public async Task<bool> UpdateAsync(ApiUserUpdateDto user)
        {
            var foundUser = await userRepository.GetByIdAsync(user.Id);
            if (foundUser == null)
            {
                return false;
            }
            
            foundUser.Email = user.Email;
            foundUser.FirstName = user.FirstName;
            foundUser.LastName = user.LastName;
            foundUser.UserName = user.Email;
            
            if (!await userRepository.UpdateAsync(foundUser))
            {
                return false;
            }

            await userRepository.RemoveRolesAsync(foundUser);
            
            ApiRole[] apiRoles = user.Roles.Select(RoleDto.ToEntity).ToArray();
            return await userRepository.AddRoleAsync(foundUser, apiRoles);
        }

        public async Task<bool> DeleteByIdAsync(string id)
        {
            return await userRepository.DeleteAsync(id);
        }

        public async Task<ApiUserDto> GetByIdAsync(string id)
        {
            var user = await userRepository.GetByIdAsync(id);
            if (user == null)
            {
                logger.LogError($"User with id {id} not found");
                return null;
            }
            
            return ApiUserDto.FromEntity(user);
        }

        public async Task<IEnumerable<ApiUserDto>> GetAllAsync()
        {
            return (await userRepository.GetAllAsync()).Select(ApiUserDto.FromEntity);
        }

        public async Task<string> GetPasswordHashAsync(string idUser)
        {
            ApiUser searchUser = await userRepository.GetByIdAsync(idUser);
            if (searchUser == null)
                return string.Empty;

            return searchUser.PasswordHash;
        }

        public async Task<object> ResetPasswordAsync(ResetPasswordDto resetPasswordInfos)
        {
            var user = await userRepository.GetByEmailAsync(resetPasswordInfos.Email);
            object response;

            //check if the user exist or not
            if (user == null)
            {
                response = new { success = false, message = "User not find, try again" };
                return response;
            }

            //reset password
            var resetPassResult = await userRepository.ResetPasswordAsync(user, resetPasswordInfos.Token, resetPasswordInfos.Password);

            if (resetPassResult.Succeeded)
            {
                response = new { success = true, message = "Password has been changed" };
                return response;
            }

            var errorsArray = resetPassResult.Errors.ToArray();
            string[] arrayErrorsStringResult = new string[errorsArray.Length];
            for (int i = 0; i < errorsArray.Length; i++)
            {
                arrayErrorsStringResult[i] = errorsArray[i].Code;
            }


            response = new { success = false, errors = arrayErrorsStringResult };
            return response;
        }

        public async Task<bool> EmailConfirmationAsync(EmailConfirmationDto emailConfirmation)
        {
            string token = Uri.UnescapeDataString(emailConfirmation.Token);
            var user = await userRepository.GetByEmailAsync(emailConfirmation.Email);
            if (user == null)
                return false;
            IdentityResult result = await userRepository.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public async Task<(bool Succeeded, string Error)> ConfirmEmailAndSetPasswordAsync(EmailConfirmationSetPasswordDto request)
        {
            try
            {
                if (request.Password != request.ConfirmPassword)
                {
                    return (false, "Les mots de passe ne correspondent pas.");
                }

                var user = await userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    return (false, "Utilisateur non trouvé.");
                }

                if (user.EmailConfirmed)
                {
                    return (false, "Cet email est déjà confirmé.");
                }

                var decodedToken = Uri.UnescapeDataString(request.Token)
                    .Replace(" ", "+");

                var confirmResult = await userRepository.ConfirmEmailAsync(user, decodedToken);
                if (!confirmResult.Succeeded)
                {
                    logger.LogError($"Email confirmation failed for user {user.Id}. Errors: {string.Join(", ", confirmResult.Errors.Select(e => e.Description))}");
                    return (false, "Le lien de confirmation n'est plus valide.");
                }

                var token = await userRepository.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await userRepository.ResetPasswordAsync(user, token, request.Password);

                if (!passwordResult.Succeeded)
                {
                    logger.LogError($"Password reset failed for user {user.Id}. Errors: {string.Join(", ", passwordResult.Errors.Select(e => e.Description))}");
                    return (false, "Le mot de passe ne respecte pas les critères de sécurité.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors de la confirmation d'email");
                return (false, "Une erreur est survenue.");
            }
        }

        public async Task<bool> SendConfirmationEmailAsync(ApiUser user, string token)
        {
            try
            {
                string tokenValidity = await settings.GetSettingValueAsync("api:email:confirmationTokenValidityLifeSpanDays", "2");
                string baseUrl = string.Concat(
                    await settings.GetSettingValueAsync("api:scheme", "https"), "://",
                    await settings.GetSettingValueAsync("api:host", "localhost"), ":",
                    await settings.GetSettingValueAsync("api:port", 5001)
                );

                var encodedToken = Uri.EscapeDataString(token);

                var parameters = new Dictionary<string, string>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "Token", encodedToken },
                    { "Email", user.Email },
                    { "TokenValidity", tokenValidity },
                    { "BaseUrl", baseUrl }
                };

                return await emailSending.SendTemplatedEmailAsync(
                    user.Email,
                    "Confirmation d'email",
                    "EmailConfirmation",
                    "fr",
                    parameters
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending confirmation email");
                return false;
            }
        }

        public async Task<ApiUserDto> GetCurrentUserAsync(ClaimsPrincipal userClaims)
        {
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                var userEmail = userClaims.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    logger.LogWarning("No user identifier found in token");
                    return null;
                }

                var userByEmail = await userRepository.GetByEmailAsync(userEmail);
                if (userByEmail == null)
                {
                    logger.LogWarning($"No user found with email: {userEmail}");
                    return null;
                }
                userId = userByEmail.Id;
            }

            return await GetByIdAsync(userId);
        }

        public async Task<bool> ResendConfirmationEmailAsync(string userEmail)
        {
            var user = await userRepository.GetByEmailAsync(userEmail);
            if (user == null)
            {
                logger.LogWarning($"User not found with email: {userEmail}");
                return false;
            }

            if (user.EmailConfirmed)
            {
                logger.LogWarning($"Email already confirmed for user: {userEmail}");
                return false;
            }

            var token = await userRepository.GenerateEmailConfirmationTokenAsync(user);
            return await SendConfirmationEmailAsync(user, token);
        }
    }
}
