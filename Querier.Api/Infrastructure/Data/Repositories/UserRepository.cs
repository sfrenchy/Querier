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
    public class UserRepository : IUserRepository
    {
        private readonly IAuthenticationRepository _authenticationRepository;
        private readonly UserManager<ApiUser> _userManager;
        private readonly RoleManager<ApiRole> _roleManager;
        private readonly ISettingService _settings;
        private readonly ILogger<UserRepository> _logger;
        private readonly IEmailSendingService _emailSending;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;

        public UserRepository(
            IAuthenticationRepository authenticationRepository,
            UserManager<ApiUser> userManager,
            RoleManager<ApiRole> roleManager,
            ISettingService settings,
            ILogger<UserRepository> logger,
            IEmailSendingService emailSending,
            IDbContextFactory<ApiDbContext> contextFactory)
        {
            _authenticationRepository = authenticationRepository ?? throw new ArgumentNullException(nameof(authenticationRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailSending = emailSending ?? throw new ArgumentNullException(nameof(emailSending));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task<(ApiUser user, List<ApiRole> roles)?> GetWithRolesAsync(string id)
        {
            try
            {
                _logger.LogInformation("Attempting to get user with roles for ID/Email: {Id}", id);
                
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("GetWithRolesAsync called with null or empty ID");
                    return null;
                }

                var user = await _userManager.FindByIdAsync(id) ?? await _userManager.FindByEmailAsync(id);

                if (user == null)
                {
                    _logger.LogWarning("User not found with ID/Email: {Id}", id);
                    return null;
                }

                var rolesString = await _userManager.GetRolesAsync(user);
                var result = _roleManager.Roles.AsNoTracking().Where(r => rolesString.Contains(r.Name)).ToList();
                _logger.LogInformation("Successfully retrieved user and roles for ID/Email: {Id}", id);
                return (user, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user with roles for ID/Email: {Id}", id);
                throw;
            }
        }

        public async Task<ApiUser> GetByIdAsync(string id)
        {
            try
            {
                _logger.LogInformation("Attempting to get user by ID: {Id}", id);

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("GetByIdAsync called with null or empty ID");
                    return null;
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User not found with ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved user with ID: {Id}", id);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user by ID: {Id}", id);
                throw;
            }
        }

        public async Task<ApiUser> GetByEmailAsync(string email)
        {
            try
            {
                _logger.LogInformation("Attempting to get user by email: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("GetByEmailAsync called with null or empty email");
                    return null;
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User not found with email: {Email}", email);
                    return null;
                }

                // Load user roles
                var roleNames = await _userManager.GetRolesAsync(user);
                
                // Load user-role associations with their roles
                using var context = await _contextFactory.CreateDbContextAsync();
                user.UserRoles = await context.Set<ApiUserRole>()
                    .AsNoTracking()
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == user.Id)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved user with email: {Email}", email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user by email: {Email}", email);
                throw;
            }
        }

        private async Task<string> GenerateRandomPassword()
        {
            // Generate a random password that meets the requirements
            var options = _userManager.Options.Password;
            var length = Math.Max(options.RequiredLength, 12); // At least 12 characters
            var nonAlphanumeric = "!@#$%^&*()";
            var numeric = "0123456789";
            var uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var lowercase = "abcdefghijklmnopqrstuvwxyz";

            var random = new Random();
            var password = new List<char>
            {
                nonAlphanumeric[random.Next(nonAlphanumeric.Length)],
                numeric[random.Next(numeric.Length)],
                uppercase[random.Next(uppercase.Length)],
                lowercase[random.Next(lowercase.Length)]
            };

            var remainingLength = length - password.Count;
            var allChars = nonAlphanumeric + numeric + uppercase + lowercase;
            
            for (int i = 0; i < remainingLength; i++)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        public async Task<IdentityResult> AddAsync(ApiUser user)
        {
            try
            {
                _logger.LogInformation("Attempting to add new user: {Email}", user?.Email);

                if (user == null)
                {
                    _logger.LogError("AddAsync called with null user");
                    return IdentityResult.Failed(new IdentityError { Description = "User cannot be null" });
                }

                string generatedPassword = await GenerateRandomPassword();
                _logger.LogDebug("Generated random password for user: {Email}", user.Email);

                var result = await _userManager.CreateAsync(user, generatedPassword);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                    return result;
                }

                try
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var tokenValidity = await _settings.GetSettingValueAsync("api:email:confirmationTokenValidityLifeSpanDays", "2");
                    var baseUrl = string.Concat(
                        await _settings.GetSettingValueAsync("api:scheme", "https"), "://",
                        await _settings.GetSettingValueAsync("api:host", "localhost"), ":",
                        await _settings.GetSettingValueAsync("api:port", "5001")
                    );

                    await _emailSending.SendTemplatedEmailAsync(
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

                    _logger.LogInformation("Successfully created user and sent confirmation email: {Email}", user.Email);
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email for user: {Email}", user.Email);
                    // We don't delete the created user, but we propagate the error
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding user: {Email}", user?.Email);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(ApiUser user)
        {
            try
            {
                _logger.LogInformation("Attempting to update user: {Email}", user?.Email);

                if (user == null)
                {
                    _logger.LogError("UpdateAsync called with null user");
                    return false;
                }

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to update user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                    return false;
                }

                _logger.LogInformation("Successfully updated user: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user: {Email}", user?.Email);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                _logger.LogInformation("Attempting to delete user with ID: {Id}", id);

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogError("DeleteAsync called with null or empty ID");
                    return false;
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User not found for deletion with ID: {Id}", id);
                    return false;
                }

                await _authenticationRepository.DeleteRefreshTokensForUserAsync(user.Id);
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully deleted user with ID: {Id}", id);
                    return true;
                }

                _logger.LogError("Failed to delete user {Id}. Errors: {@Errors}", id, result.Errors);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<ApiUser>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users");
                var users = await _userManager.Users.AsNoTracking().ToListAsync();
                _logger.LogInformation("Successfully retrieved {Count} users", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all users");
                throw;
            }
        }

        public async Task<bool> AddRoleAsync(ApiUser user, ApiRole[] roles)
        {
            try
            {
                _logger.LogInformation("Attempting to add roles for user: {Email}", user.Email);

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    _logger.LogDebug("Removing existing roles for user: {Email}", user.Email);
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        _logger.LogError("Failed to remove existing roles for user {Email}. Errors: {@Errors}", 
                            user.Email, removeResult.Errors);
                        return false;
                    }
                }

                foreach (var role in roles)
                {
                    if (string.IsNullOrEmpty(role.Name))
                    {
                        _logger.LogWarning("Skipping role with null or empty name for user: {Email}", user.Email);
                        continue;
                    }

                    var addResult = await _userManager.AddToRoleAsync(user, role.Name);
                    if (!addResult.Succeeded)
                    {
                        _logger.LogError("Failed to add role {Role} to user {Email}. Errors: {@Errors}", 
                            role.Name, user.Email, addResult.Errors);
                        return false;
                    }
                }

                _logger.LogInformation("Successfully updated roles for user: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update roles for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> RemoveRolesAsync(ApiUser user)
        {
            try
            {
                _logger.LogInformation("Attempting to remove all roles from user: {Email}", user.Email);

                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Any())
                {
                    _logger.LogInformation("No roles to remove for user: {Email}", user.Email);
                    return true;
                }

                var result = await _userManager.RemoveFromRolesAsync(user, userRoles);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to remove roles from user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                    return false;
                }

                _logger.LogInformation("Successfully removed all roles from user: {Email}", user.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove roles from user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<IdentityResult> ResetPasswordAsync(ApiUser user, string token, string password)
        {
            try
            {
                _logger.LogInformation("Attempting to reset password for user: {Email}", user.Email);
                var result = await _userManager.ResetPasswordAsync(user, token, password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully reset password for user: {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to reset password for user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<IdentityResult> ConfirmEmailAsync(ApiUser user, string token)
        {
            try
            {
                _logger.LogInformation("Attempting to confirm email for user: {Email}", user.Email);
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully confirmed email for user: {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to confirm email for user {Email}. Errors: {@Errors}", 
                        user.Email, result.Errors);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to confirm email for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApiUser user)
        {
            try
            {
                _logger.LogInformation("Generating password reset token for user: {Email}", user.Email);
                return await _userManager.GeneratePasswordResetTokenAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate password reset token for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<string> GenerateEmailConfirmationTokenAsync(ApiUser user)
        {
            try
            {
                _logger.LogInformation("Generating email confirmation token for user: {Email}", user.Email);
                return await _userManager.GenerateEmailConfirmationTokenAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate email confirmation token for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<List<ApiRole>> GetRolesAsync(ApiUser user)
        {
            try
            {
                _logger.LogInformation("Retrieving roles for user: {Email}", user.Email);
                var roleNames = await _userManager.GetRolesAsync(user);

                var roles = _roleManager.Roles.AsNoTracking().Where(r => roleNames.Contains(r.Name)).ToList();
                _logger.LogInformation("Successfully retrieved {Count} roles for user: {Email}", roles.Count, user.Email);
                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve roles for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> CheckPasswordAsync(ApiUser user, string password)
        {
            try
            {
                _logger.LogInformation("Checking password for user: {Email}", user.Email);
                return await _userManager.CheckPasswordAsync(user, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check password for user: {Email}", user.Email);
                throw;
            }
        }

        public async Task<bool> IsEmailConfirmedAsync(ApiUser user)
        {
            try
            {
                _logger.LogInformation("Checking email confirmation status for user: {Email}", user.Email);
                return await _userManager.IsEmailConfirmedAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check email confirmation status for user: {Email}", user.Email);
                throw;
            }
        }
    }
}
