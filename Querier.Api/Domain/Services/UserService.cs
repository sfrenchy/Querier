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
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    logger.LogError("Attempted to set roles with null or empty user ID");
                    return false;
                }

                if (roles == null)
                {
                    logger.LogError("Attempted to set null roles for user {UserId}", id);
                    return false;
                }

                logger.LogInformation("Setting roles for user {UserId}", id);
            var foundUser = await userRepository.GetByIdAsync(id);
            if (foundUser == null)
                {
                    logger.LogWarning("User with ID {UserId} not found", id);
                    return false;
                }
                
                logger.LogDebug("Removing existing roles for user {UserId}", id);
                await userRepository.RemoveRolesAsync(foundUser);

                logger.LogDebug("Adding {Count} roles to user {UserId}", roles.Count, id);
                ApiRole[] apiRoles = roles.Select(RoleDto.ToEntity).ToArray();
                var result = await userRepository.AddRoleAsync(foundUser, apiRoles);

                if (result)
                {
                    logger.LogInformation("Successfully updated roles for user {UserId}", id);
                }
                else
                {
                    logger.LogWarning("Failed to update roles for user {UserId}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting roles for user {UserId}", id);
                return false;
            }
        }

        public async Task<bool> AddAsync(ApiUserCreateDto user)
        {
            try
            {
                if (user == null)
                {
                    logger.LogError("Attempted to add null user");
                    return false;
                }

                logger.LogInformation("Adding new user with email {Email}", user.Email);
            var foundUser = await userRepository.GetByEmailAsync(user.Email);
            if (foundUser != null)
            {
                    logger.LogWarning("User with email {Email} already exists", user.Email);
                    return false;
                }

                ApiUser newUser = ApiUserDto.ToEntity(user);
                var result = await userRepository.AddAsync(newUser);
                if (result != IdentityResult.Success)
                {
                    logger.LogError("Failed to create user {Email}. Errors: {Errors}", 
                        user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
                }

                logger.LogDebug("Adding roles to new user {Email}", user.Email);
                var roles = roleRepository.GetAll().Where(r => user.Roles.Contains(r.Name)).ToArray();
                var roleResult = await userRepository.AddRoleAsync(newUser, roles);

                if (roleResult)
                {
                    logger.LogInformation("Successfully created user {Email} with roles", user.Email);
                }
                else
                {
                    logger.LogWarning("Created user {Email} but failed to add roles", user.Email);
                }

                return roleResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding user {Email}", user?.Email);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(ApiUserUpdateDto user)
        {
            try
            {
                if (user == null)
                {
                    logger.LogError("Attempted to update null user");
                    return false;
                }

                logger.LogInformation("Updating user {UserId}", user.Id);
            var foundUser = await userRepository.GetByIdAsync(user.Id);
            if (foundUser == null)
            {
                    logger.LogWarning("User with ID {UserId} not found", user.Id);
                return false;
            }
            
            foundUser.Email = user.Email;
            foundUser.FirstName = user.FirstName;
            foundUser.LastName = user.LastName;
            foundUser.UserName = user.Email;
            
                var updateResult = await userRepository.UpdateAsync(foundUser);
                if (!updateResult)
                {
                    logger.LogError("Failed to update user {UserId}", user.Id);
                    return false;
                }

                logger.LogDebug("Updating roles for user {UserId}", user.Id);
                await userRepository.RemoveRolesAsync(foundUser);
                
                ApiRole[] apiRoles = user.Roles.Select(RoleDto.ToEntity).ToArray();
                var roleResult = await userRepository.AddRoleAsync(foundUser, apiRoles);

                if (roleResult)
                {
                    logger.LogInformation("Successfully updated user {UserId} with roles", user.Id);
                }
                else
                {
                    logger.LogWarning("Updated user {UserId} but failed to update roles", user.Id);
                }

                return roleResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating user {UserId}", user?.Id);
                return false;
            }
        }

        public async Task<bool> DeleteByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    logger.LogError("Attempted to delete user with null or empty ID");
                    return false;
                }

                logger.LogInformation("Deleting user {UserId}", id);
                var result = await userRepository.DeleteAsync(id);

                if (result)
                {
                    logger.LogInformation("Successfully deleted user {UserId}", id);
                }
                else
                {
                    logger.LogWarning("Failed to delete user {UserId}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting user {UserId}", id);
                return false;
            }
        }

        public async Task<ApiUserDto> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    logger.LogError("Attempted to get user with null or empty ID");
                    return null;
                }

                logger.LogDebug("Retrieving user {UserId}", id);
            var user = await userRepository.GetByIdAsync(id);
            if (user == null)
                {
                    logger.LogWarning("User with ID {UserId} not found", id);
                    return null;
                }
                
                var dto = ApiUserDto.FromEntity(user);
                logger.LogDebug("Successfully retrieved user {UserId}", id);
                return dto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving user {UserId}", id);
                return null;
            }
        }

        public async Task<IEnumerable<ApiUserDto>> GetAllAsync()
        {
            try
            {
                logger.LogInformation("Retrieving all users");
                var users = await userRepository.GetAllAsync();
                var dtos = users.Select(ApiUserDto.FromEntity);
                logger.LogInformation("Retrieved {Count} users", users.Count());
                return dtos;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all users");
                return [];
            }
        }

        public async Task<string> GetPasswordHashAsync(string idUser)
        {
            try
            {
                if (string.IsNullOrEmpty(idUser))
                {
                    logger.LogError("Attempted to get password hash with null or empty user ID");
                    return string.Empty;
                }

                logger.LogDebug("Retrieving password hash for user {UserId}", idUser);
                var user = await userRepository.GetByIdAsync(idUser);
                if (user == null)
                {
                    logger.LogWarning("User with ID {UserId} not found", idUser);
                    return string.Empty;
                }

                return user.PasswordHash ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving password hash for user {UserId}", idUser);
                return string.Empty;
            }
        }

        public async Task<object> ResetPasswordAsync(ResetPasswordDto resetPasswordInfos)
        {
            try
            {
                if (resetPasswordInfos == null)
                {
                    logger.LogError("Attempted to reset password with null request");
                    return new { success = false, message = "Invalid request" };
                }

                logger.LogInformation("Resetting password for user {Email}", resetPasswordInfos.Email);
            var user = await userRepository.GetByEmailAsync(resetPasswordInfos.Email);

            if (user == null)
            {
                    logger.LogWarning("User not found for password reset: {Email}", resetPasswordInfos.Email);
                    return new { success = false, message = "User not found, try again" };
                }

                var result = await userRepository.ResetPasswordAsync(
                    user, 
                    resetPasswordInfos.Token, 
                    resetPasswordInfos.Password
                );

                if (result.Succeeded)
                {
                    logger.LogInformation("Successfully reset password for user {Email}", resetPasswordInfos.Email);
                    return new { success = true, message = "Password has been changed" };
                }

                var errors = result.Errors.Select(e => e.Code).ToArray();
                logger.LogWarning("Password reset failed for user {Email}. Errors: {Errors}", 
                    resetPasswordInfos.Email, string.Join(", ", errors));
                return new { success = false, errors };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resetting password for user {Email}", resetPasswordInfos?.Email);
                return new { success = false, message = "An unexpected error occurred" };
            }
        }

        public async Task<bool> EmailConfirmationAsync(EmailConfirmationDto emailConfirmation)
        {
            try
            {
                if (emailConfirmation == null)
                {
                    logger.LogError("Attempted email confirmation with null request");
                    return false;
                }

                logger.LogInformation("Processing email confirmation for {Email}", emailConfirmation.Email);
            string token = Uri.UnescapeDataString(emailConfirmation.Token);
            var user = await userRepository.GetByEmailAsync(emailConfirmation.Email);
                
            if (user == null)
                {
                    logger.LogWarning("User not found for email confirmation: {Email}", emailConfirmation.Email);
                    return false;
                }

                var result = await userRepository.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    logger.LogInformation("Successfully confirmed email for user {Email}", emailConfirmation.Email);
                }
                else
                {
                    logger.LogWarning("Email confirmation failed for user {Email}. Errors: {Errors}", 
                        emailConfirmation.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error confirming email for user {Email}", emailConfirmation?.Email);
                return false;
            }
        }

        public async Task<(bool Succeeded, string Error)> ConfirmEmailAndSetPasswordAsync(EmailConfirmationSetPasswordDto request)
        {
            try
            {
                if (request == null)
                {
                    logger.LogError("Attempted email confirmation with null request");
                    return (false, "Invalid request");
                }

                if (request.Password != request.ConfirmPassword)
                {
                    logger.LogWarning("Password mismatch for user {Email}", request.Email);
                    return (false, "Les mots de passe ne correspondent pas.");
                }

                logger.LogInformation("Processing email confirmation and password set for {Email}", request.Email);
                var user = await userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    logger.LogWarning("User not found: {Email}", request.Email);
                    return (false, "Utilisateur non trouvé.");
                }

                if (user.EmailConfirmed)
                {
                    logger.LogWarning("Email already confirmed for user {Email}", request.Email);
                    return (false, "Cet email est déjà confirmé.");
                }

                var decodedToken = Uri.UnescapeDataString(request.Token)
                    .Replace(" ", "+");

                var confirmResult = await userRepository.ConfirmEmailAsync(user, decodedToken);
                if (!confirmResult.Succeeded)
                {
                    logger.LogError("Email confirmation failed for user {UserId}. Errors: {Errors}", 
                        user.Id, string.Join(", ", confirmResult.Errors.Select(e => e.Description)));
                    return (false, "Le lien de confirmation n'est plus valide.");
                }

                var token = await userRepository.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await userRepository.ResetPasswordAsync(user, token, request.Password);

                if (!passwordResult.Succeeded)
                {
                    logger.LogError("Password reset failed for user {UserId}. Errors: {Errors}", 
                        user.Id, string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    return (false, "Le mot de passe ne respecte pas les critères de sécurité.");
                }

                logger.LogInformation("Successfully confirmed email and set password for user {Email}", request.Email);
                return (true, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error confirming email and setting password for user {Email}", request?.Email);
                return (false, "Une erreur est survenue.");
            }
        }

        public async Task<bool> SendConfirmationEmailAsync(ApiUser user, string token)
        {
            try
            {
                if (user == null)
                {
                    logger.LogError("Attempted to send confirmation email to null user");
                    return false;
                }

                if (string.IsNullOrEmpty(token))
                {
                    logger.LogError("Attempted to send confirmation email with null token for user {Email}", user.Email);
                    return false;
                }

                logger.LogInformation("Preparing confirmation email for user {Email}", user.Email);

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

                logger.LogDebug("Sending confirmation email to {Email}", user.Email);
                var result = await emailSending.SendTemplatedEmailAsync(
                    user.Email,
                    "Confirmation d'email",
                    "EmailConfirmation",
                    "fr",
                    parameters
                );

                if (result)
                {
                    logger.LogInformation("Successfully sent confirmation email to {Email}", user.Email);
                }
                else
                {
                    logger.LogWarning("Failed to send confirmation email to {Email}", user.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending confirmation email to {Email}", user?.Email);
                return false;
            }
        }

        public async Task<ApiUserDto> GetCurrentUserAsync(ClaimsPrincipal userClaims)
        {
            try
            {
                if (userClaims == null)
                {
                    logger.LogError("Attempted to get current user with null claims");
                    return null;
                }
                
                var userEmail = userClaims.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    logger.LogWarning("No user identifier found in token");
                    return null;
                }

                logger.LogDebug("Looking up user by email: {Email}", userEmail);
                var userByEmail = await userRepository.GetByEmailAsync(userEmail);
                if (userByEmail == null)
                {
                    logger.LogWarning("No user found with email: {Email}", userEmail);
                    return null;
                }

                return ApiUserDto.FromEntity(userByEmail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<bool> ResendConfirmationEmailAsync(string userEmail)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail))
                {
                    logger.LogError("Attempted to resend confirmation email with null or empty email");
                    return false;
                }

                logger.LogInformation("Resending confirmation email to {Email}", userEmail);
            var user = await userRepository.GetByEmailAsync(userEmail);
            if (user == null)
            {
                    logger.LogWarning("User not found with email: {Email}", userEmail);
                return false;
            }

            if (user.EmailConfirmed)
            {
                    logger.LogWarning("Email already confirmed for user: {Email}", userEmail);
                return false;
            }

            var token = await userRepository.GenerateEmailConfirmationTokenAsync(user);
                var result = await SendConfirmationEmailAsync(user, token);

                if (result)
                {
                    logger.LogInformation("Successfully resent confirmation email to {Email}", userEmail);
                }
                else
                {
                    logger.LogWarning("Failed to resend confirmation email to {Email}", userEmail);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resending confirmation email to {Email}", userEmail);
                return false;
            }
        }
    }
}
