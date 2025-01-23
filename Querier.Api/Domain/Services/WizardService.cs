using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.DTOs.Requests.Setup;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Domain.Services
{
    public class WizardService(
        UserManager<ApiUser> userManager,
        RoleManager<ApiRole> roleManager,
        ISettingService settingService,
        ILogger<WizardService> logger)
        : IWizardService, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed;

        public async Task<(bool Success, string Error)> SetupAsync(SetupDto request)
        {
            try
            {
                logger.LogInformation("Starting application setup process");

                if (request == null)
                {
                    logger.LogError("Setup request is null");
                    return (false, "Setup configuration data is required");
                }

                if (request.Admin == null)
                {
                    logger.LogError("Admin configuration is missing");
                    return (false, "Admin configuration is required");
                }

                logger.LogDebug("Attempting to acquire setup lock");
                if (!await _semaphore.WaitAsync(TimeSpan.FromMinutes(1)))
                {
                    logger.LogError("Failed to acquire setup lock - timeout occurred");
                    return (false, "Setup lock timeout - another setup process might be running");
                }

                try
                {
                    logger.LogInformation("Configuring JWT settings");
                    var jwtSecret = GenerateSecureSecret();
                    
                    await UpdateJwtSettings(jwtSecret);
                    await ConfigureAdminRole();
                    await CreateAdminUser(request.Admin);
                    await ConfigureSmtpSettings(request.Smtp);
                    await UpdateApiConfiguration();

                    logger.LogInformation("Application setup completed successfully");
                    return (true, null);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Setup process failed");
                    return (false, $"Setup failed: {ex.Message}");
                }
                finally
                {
                    _semaphore.Release();
                    logger.LogDebug("Setup lock released");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during setup process");
                return (false, $"Unexpected error during setup: {ex.Message}");
            }
        }

        private async Task UpdateJwtSettings(string jwtSecret)
        {
            try
            {
                logger.LogDebug("Updating JWT settings");
                await settingService.UpdateSettingIfExistsAsync("jwt:secret", jwtSecret);
                await settingService.UpdateSettingIfExistsAsync("jwt:issuer", "QuerierApi");
                await settingService.UpdateSettingIfExistsAsync("jwt:audience", "QuerierClient");
                await settingService.UpdateSettingIfExistsAsync("jwt:expiry", 60);
                logger.LogInformation("JWT settings updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update JWT settings");
                throw;
            }
        }

        private async Task ConfigureAdminRole()
        {
            try
            {
                logger.LogDebug("Checking if Admin role exists");
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    logger.LogInformation("Creating Admin role");
                    var adminRole = new ApiRole
                    {
                        Name = "Admin",
                        NormalizedName = "ADMIN"
                    };
                    var createRoleResult = await roleManager.CreateAsync(adminRole);
                    if (!createRoleResult.Succeeded)
                    {
                        var errors = string.Join(", ", createRoleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                        logger.LogError("Failed to create Admin role: {Errors}", errors);
                        throw new InvalidOperationException($"Failed to create Admin role: {errors}");
                    }
                }
                logger.LogInformation("Admin role configuration completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure Admin role");
                throw;
            }
        }

        private async Task CreateAdminUser(SetupAdminDto adminSetup)
        {
            try
            {
                logger.LogInformation("Creating admin user with email: {Email}", adminSetup.Email);
                
                var existingUser = await userManager.FindByEmailAsync(adminSetup.Email);
                if (existingUser != null)
                {
                    logger.LogWarning("User with email {Email} already exists", adminSetup.Email);
                    throw new InvalidOperationException("User already exists");
                }

                var adminUser = new ApiUser
                {
                    UserName = adminSetup.Email,
                    Email = adminSetup.Email,
                    FirstName = adminSetup.FirstName,
                    LastName = adminSetup.Name,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, adminSetup.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogError("Failed to create admin user: {Errors}", errors);
                    throw new InvalidOperationException($"Failed to create admin user: {errors}");
                }

                logger.LogDebug("Assigning Admin role to user");
                var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    logger.LogError("Failed to assign Admin role: {Errors}", errors);
                    throw new InvalidOperationException($"Failed to assign Admin role: {errors}");
                }

                logger.LogInformation("Admin user created and configured successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create admin user");
                throw;
            }
        }

        private async Task ConfigureSmtpSettings(SetupSmtpDto smtpSetup)
        {
            try
            {
                logger.LogDebug("Configuring SMTP settings");
                await settingService.UpdateSettingIfExistsAsync("smtp:host", smtpSetup.Host);
                await settingService.UpdateSettingIfExistsAsync("smtp:port", smtpSetup.Port);
                await settingService.UpdateSettingIfExistsAsync("smtp:username", smtpSetup.Username);
                await settingService.UpdateSettingIfExistsAsync("smtp:useSSL", smtpSetup.useSSL);
                await settingService.UpdateSettingIfExistsAsync("smtp:senderEmail", smtpSetup.SenderEmail);
                await settingService.UpdateSettingIfExistsAsync("smtp:senderName", smtpSetup.SenderName);
                await settingService.UpdateSettingIfExistsAsync("smtp:requiresAuth", smtpSetup.RequireAuth);
                logger.LogInformation("SMTP settings configured successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure SMTP settings");
                throw;
            }
        }

        private async Task UpdateApiConfiguration()
        {
            try
            {
                logger.LogDebug("Updating API configuration status");
                var isConfigured = await settingService.GetSettingValueIfExistsAsync("api:isConfigured", false, "");
                if (!isConfigured)
                {
                    await settingService.UpdateSettingIfExistsAsync("api:isConfigured", true);
                }
                logger.LogInformation("API configuration status updated successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update API configuration status");
                throw;
            }
        }

        private string GenerateSecureSecret(int length = 32)
        {
            try
            {
                logger.LogDebug("Generating secure secret with length: {Length}", length);
                using var rng = new System.Security.Cryptography.RNGCryptoServiceProvider();
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                var secret = Convert.ToBase64String(bytes);
                logger.LogDebug("Secure secret generated successfully");
                return secret;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate secure secret");
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    logger.LogDebug("Disposing WizardService resources");
                    _semaphore.Dispose();
                }
                _disposed = true;
            }
        }

        ~WizardService()
        {
            Dispose(false);
        }
    }
}