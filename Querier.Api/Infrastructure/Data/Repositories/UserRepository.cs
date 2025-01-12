using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class UserRepository(
        IAuthenticationRepository authenticationRepository,
        UserManager<ApiUser> userManager,
        RoleManager<ApiRole> roleManager,
        ISettingService settings,
        ILogger<UserRepository> logger,
        IEmailSendingService emailSending,
        ApiDbContext context)
        : IUserRepository
    {
        public async Task<(ApiUser user, List<ApiRole> roles)?> GetWithRolesAsync(string id)
        {
            try
            {
                logger.LogInformation("Attempting to get user with roles for ID/Email: {Id}", id);
                
                if (string.IsNullOrEmpty(id))
                {
                    logger.LogWarning("GetWithRolesAsync called with null or empty ID");
                    return null;
                }

                var user = await userManager.FindByIdAsync(id) ?? await userManager.FindByEmailAsync(id);

                if (user == null)
                {
                    logger.LogWarning("User not found with ID/Email: {Id}", id);
                    return null;
                }

                var rolesString = await userManager.GetRolesAsync(user);
                var result = roleManager.Roles.Where(r => rolesString.Contains(r.Name)).ToList();
                logger.LogInformation("Successfully retrieved user and roles for ID/Email: {Id}", id);
                return (user, result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get user with roles for ID/Email: {Id}", id);
                throw;
            }
        }

        public async Task<ApiUser> GetByIdAsync(string id)
        {
            try
            {
                logger.LogInformation("Attempting to get user by ID: {Id}", id);

                if (string.IsNullOrEmpty(id))
                {
                    logger.LogWarning("GetByIdAsync called with null or empty ID");
                    return null;
                }

                var user = await userManager.FindByIdAsync(id);
                if (user == null)
                {
                    logger.LogWarning("User not found with ID: {Id}", id);
                    return null;
                }

                logger.LogInformation("Successfully retrieved user with ID: {Id}", id);
                return user;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get user by ID: {Id}", id);
                throw;
            }
        }

        public async Task<ApiUser> GetByEmailAsync(string email)
        {
            try
            {
                logger.LogInformation("Attempting to get user by email: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    logger.LogWarning("GetByEmailAsync called with null or empty email");
                    return null;
                }

                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    logger.LogWarning("User not found with email: {Email}", email);
                    return null;
                }

                // Load user roles
                var roleNames = await userManager.GetRolesAsync(user);
                
                // Charge les associations utilisateur-rôle existantes avec leurs rôles
                user.UserRoles = await context.Set<ApiUserRole>()
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == user.Id)
                    .ToListAsync();

                logger.LogInformation("Successfully retrieved user with email: {Email}", email);
                return user;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get user by email: {Email}", email);
                throw;
            }
        }

        public async Task<IdentityResult> AddAsync(ApiUser user)
        {
            try
            {
                logger.LogInformation("Attempting to add new user: {Email}", user?.Email);

                if (user == null)
                {
                    logger.LogError("AddAsync called with null user");
                    return IdentityResult.Failed(new IdentityError { Description = "User cannot be null" });
            }

            string generatedPassword = await GenerateRandomPassword();
                logger.LogDebug("Generated random password for user: {Email}", user.Email);

                var result = await userManager.CreateAsync(user, generatedPassword);
            if (!result.Succeeded)
            {
                    logger.LogError("Failed to create user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                return result;
            }

                try
                {
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var tokenValidity = await settings.GetSettingValueAsync("api:email:confirmationTokenValidityLifeSpanDays", "2");
                    var baseUrl = string.Concat(
                        await settings.GetSettingValueAsync("api:scheme", "https"), "://",
                        await settings.GetSettingValueAsync("api:host", "localhost"), ":",
                        await settings.GetSettingValueAsync("api:port", "5001")
                    );

                    await emailSending.SendTemplatedEmailAsync(
                user.Email,
                "Confirmation de votre email",
                "EmailConfirmation",
                "en",
                new Dictionary<string, string> { 
                    { "Token", token }, 
                    { "TokenValidity", tokenValidity }, 
                    { "BaseUrl", baseUrl },
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "Email", user.Email }
                }
            );

                    logger.LogInformation("Successfully created user and sent confirmation email: {Email}", user.Email);
            return result;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send confirmation email for user: {Email}", user.Email);
                    // On ne supprime pas l'utilisateur créé, mais on propage l'erreur
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while adding user: {Email}", user?.Email);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(ApiUser user)
        {
            try
            {
                logger.LogInformation("Attempting to update user: {Email}", user?.Email);

                if (user == null)
                {
                    logger.LogError("UpdateAsync called with null user");
                    return false;
                }

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to update user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                    return false;
                }

                logger.LogInformation("Successfully updated user: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update user: {Email}", user?.Email);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                logger.LogInformation("Attempting to delete user with ID: {Id}", id);

                if (string.IsNullOrEmpty(id))
                {
                    logger.LogError("DeleteAsync called with null or empty ID");
                    return false;
                }

                var user = await userManager.FindByIdAsync(id);
                if (user == null)
                {
                    logger.LogWarning("User not found for deletion with ID: {Id}", id);
                    return false;
                }

                await authenticationRepository.DeleteRefreshTokensForUserAsync(user.Id);
                var result = await userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    logger.LogInformation("Successfully deleted user with ID: {Id}", id);
                    return true;
                }

                logger.LogError("Failed to delete user {Id}. Errors: {@Errors}", 
                    id, result.Errors);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete user with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<ApiUser>> GetAllAsync()
        {
            try
            {
                logger.LogInformation("Retrieving all users");
                var users = await userManager.Users.ToListAsync();
                logger.LogInformation("Successfully retrieved {Count} users", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve all users");
                throw;
            }
        }

        public async Task<bool> AddRoleAsync(ApiUser user, ApiRole[] roles)
        {
            try
            {
                logger.LogInformation("Attempting to add roles for user: {Email}", user.Email);

                var currentRoles = await userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    logger.LogDebug("Removing existing roles for user: {Email}", user.Email);
                    var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        logger.LogError("Failed to remove existing roles for user {Email}. Errors: {@Errors}", 
                            user.Email, removeResult.Errors);
                        return false;
                    }
                }

                foreach (var role in roles)
                {
                    if (string.IsNullOrEmpty(role.Name))
                    {
                        logger.LogWarning("Skipping role with null or empty name for user: {Email}", user.Email);
                        continue;
                    }

                    var addResult = await userManager.AddToRoleAsync(user, role.Name);
                    if (!addResult.Succeeded)
                    {
                        logger.LogError("Failed to add role {Role} to user {Email}. Errors: {@Errors}", 
                            role.Name, user.Email, addResult.Errors);
                        return false;
                    }
                }

                logger.LogInformation("Successfully updated roles for user: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update roles for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> RemoveRolesAsync(ApiUser user)
        {
            try
            {
                logger.LogInformation("Attempting to remove all roles from user: {Email}", user.Email);

                var userRoles = await userManager.GetRolesAsync(user);
                if (!userRoles.Any())
                {
                    logger.LogInformation("No roles to remove for user: {Email}", user.Email);
                    return true;
                }

                var result = await userManager.RemoveFromRolesAsync(user, userRoles);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to remove roles from user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                    return false;
                }

                logger.LogInformation("Successfully removed all roles from user: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove roles from user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<IdentityResult> ResetPasswordAsync(ApiUser user, string token, string password)
        {
            try
            {
                logger.LogInformation("Attempting to reset password for user: {Email}", user.Email);
                var result = await userManager.ResetPasswordAsync(user, token, password);

                if (result.Succeeded)
                {
                    logger.LogInformation("Successfully reset password for user: {Email}", user.Email);
                }
                else
                {
                    logger.LogWarning("Failed to reset password for user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reset password for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<IdentityResult> ConfirmEmailAsync(ApiUser user, string token)
        {
            try
            {
                logger.LogInformation("Attempting to confirm email for user: {Email}", user.Email);
                var result = await userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    logger.LogInformation("Successfully confirmed email for user: {Email}", user.Email);
                }
                else
                {
                    logger.LogWarning("Failed to confirm email for user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to confirm email for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApiUser user)
        {
            try
            {
                logger.LogInformation("Generating password reset token for user: {Email}", user.Email);
                return await userManager.GeneratePasswordResetTokenAsync(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate password reset token for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(ApiUser user)
        {
            try
            {
                logger.LogInformation("Generating email confirmation token for user: {Email}", user.Email);
                return await userManager.GenerateEmailConfirmationTokenAsync(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate email confirmation token for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<List<ApiRole>> GetRolesAsync(ApiUser user)
        {
            try
            {
                logger.LogInformation("Retrieving roles for user: {Email}", user.Email);
                var roleNames = await userManager.GetRolesAsync(user);
                var roles = roleManager.Roles.Where(r => roleNames.Contains(r.Name)).ToList();
                logger.LogInformation("Successfully retrieved {Count} roles for user: {Email}", roles.Count, user.Email);
                return roles;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve roles for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> CheckPasswordAsync(ApiUser user, string password)
        {
            try
            {
                logger.LogInformation("Checking password for user: {Email}", user.Email);
                return await userManager.CheckPasswordAsync(user, password);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check password for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> IsEmailConfirmedAsync(ApiUser user)
        {
            try
            {
                logger.LogInformation("Checking email confirmation status for user: {Email}", user.Email);
                return await userManager.IsEmailConfirmedAsync(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check email confirmation status for user: {Email}", user.Email);
                throw;
            }
        }

        private async Task<string> GenerateRandomPassword()
        {
            try
            {
                logger.LogDebug("Generating random password");

                var opts = new PasswordOptions()
                {
                    RequireDigit = await settings.GetSettingValueAsync("api:password:requireDigit", true),
                    RequireLowercase = await settings.GetSettingValueAsync("api:password:requireLowercase", true),
                    RequireNonAlphanumeric = await settings.GetSettingValueAsync("api:password:requireNonAlphanumeric", true),
                    RequireUppercase = await settings.GetSettingValueAsync("api:password:requireUppercase", true),
                    RequiredLength = await settings.GetSettingValueAsync("api:password:requiredLength", 12),
                    RequiredUniqueChars = await settings.GetSettingValueAsync("api:password:requiredUniqueChars", 1)
                };

                string[] randomChars =
                [
                "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
                "abcdefghijkmnopqrstuvwxyz",    // lowercase
                "0123456789",                   // digits
                "!@$?_-"                        // non-alphanumeric
                ];

                var rand = new Random(Environment.TickCount);
                var chars = new List<char>();

            if (opts.RequireUppercase)
                chars.Insert(rand.Next(0, chars.Count), 
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);

            if (opts.RequireLowercase)
                chars.Insert(rand.Next(0, chars.Count), 
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);

            if (opts.RequireDigit)
                chars.Insert(rand.Next(0, chars.Count), 
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);

            if (opts.RequireNonAlphanumeric)
                chars.Insert(rand.Next(0, chars.Count), 
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count; i < opts.RequiredLength
                                      || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count), 
                    rcs[rand.Next(0, rcs.Length)]);
            }

                logger.LogDebug("Successfully generated random password");
            return new string(chars.ToArray());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate random password");
                throw;
            }
        }
    }
}
