using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Common.Metadata;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Domain.Services
{
    public class WizardService : IWizardService, IDisposable
    {
        private readonly UserManager<ApiUser> _userManager;
        private readonly RoleManager<ApiRole> _roleManager;
        private readonly ISettingService _settingService;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<WizardService> _logger;
        private bool _disposed = false;
        public WizardService(
            UserManager<ApiUser> userManager,
            RoleManager<ApiRole> roleManager,
            ISettingService settingService,
            ILogger<WizardService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _settingService = settingService;
            _logger = logger;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task<(bool Success, string Error)> SetupAsync(SetupDto request)
        {
            try
            {
                _logger.LogInformation("Acquiring setup lock...");
                if (!await _semaphore.WaitAsync(TimeSpan.FromMinutes(1)))
                {
                    return (false, "Setup lock timeout");
                }

                try
                {
                    using var context = await _contextFactory.CreateDbContextAsync();

                    // Configuration JWT
                    _logger.LogInformation("Configuring JWT settings...");
                    var jwtSecret = GenerateSecureSecret();
                    
                    // Utiliser UpdateSettingIfExists pour éviter les conflits de clés uniques
                    await _settingService.UpdateSettingIfExistsAsync("jwt:secret", jwtSecret, "");
                    await _settingService.UpdateSettingIfExistsAsync("jwt:issuer", "QuerierApi", "");
                    await _settingService.UpdateSettingIfExistsAsync("jwt:audience", "QuerierClient", "");
                    await _settingService.UpdateSettingIfExistsAsync("jwt:expiry", 60, "");

                    if (!await _roleManager.RoleExistsAsync("Admin"))
                    {
                        _logger.LogInformation("Creating Admin role...");
                        var adminRole = new ApiRole
                        {
                            Name = "Admin",
                            NormalizedName = "ADMIN"
                        };
                        var createRoleResult = await _roleManager.CreateAsync(adminRole);
                        if (!createRoleResult.Succeeded)
                        {
                            return (false, "Failed to create Admin role");
                        }
                    }

                    _logger.LogInformation("Creating admin user with email: {Email}", request.Admin.Email);
                    var existingUser = await _userManager.FindByEmailAsync(request.Admin.Email);
                    if (existingUser != null)
                    {
                        _logger.LogWarning("User with email {Email} already exists", request.Admin.Email);
                        return (false, "User already exists");
                    }

                    var adminUser = new ApiUser
                    {
                        UserName = request.Admin.Email,
                        Email = request.Admin.Email,
                        FirstName = request.Admin.FirstName,
                        LastName = request.Admin.Name,
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(adminUser, request.Admin.Password);
                    if (!createResult.Succeeded)
                    {
                        var errors = createResult.Errors.Select(e => $"{e.Code}: {e.Description}");
                        _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", errors));
                        return (false, "Failed to create admin user: " + string.Join(", ", errors));
                    }

                    _logger.LogInformation("Assigning admin role...");
                    var roleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
                    if (!roleResult.Succeeded)
                    {
                        var errors = roleResult.Errors.Select(e => $"{e.Code}: {e.Description}");
                        _logger.LogError("Failed to assign admin role: {Errors}", string.Join(", ", errors));
                        return (false, "Failed to assign admin role: " + string.Join(", ", errors));
                    }

                    _logger.LogInformation("Configuring SMTP settings...");
                    await _settingService.UpdateSettingIfExistsAsync("smtp:host", request.Smtp.Host, "");
                    await _settingService.UpdateSettingIfExistsAsync("smtp:port", request.Smtp.Port, "");
                    await _settingService.UpdateSettingIfExistsAsync("smtp:username", request.Smtp.Username, "");
                    await _settingService.UpdateSettingIfExistsAsync("smtp:password", request.Smtp.Password, "");
                    await _settingService.UpdateSettingIfExistsAsync("smtp:useSSL", request.Smtp.useSSL, "");
                    await _settingService.UpdateSettingIfExistsAsync("smtp:senderEmail", request.Smtp.SenderEmail, "");
                    await _settingService.UpdateSettingIfExistsAsync("smtp:senderName", request.Smtp.SenderName, "");
                    await _settingService.UpdateSettingIfExistsAsync("smtp:requiresAuth", request.Smtp.RequireAuth, "");

                    var isConfiguredSetting = await context.Settings
                        .FirstOrDefaultAsync(s => s.Name == "api:isConfigured");

                    if (isConfiguredSetting != null)
                    {
                        _logger.LogInformation("Updating existing isConfigured setting");
                        isConfiguredSetting.Value = "true";
                        context.Settings.Update(isConfiguredSetting);
                    }
                    else
                    {
                        _logger.LogInformation("Creating new isConfigured setting");
                        await context.Settings.AddAsync(new Setting
                        {
                            Name = "api:isConfigured",
                            Value = "true"
                        });
                    }

                    await context.SaveChangesAsync();
                    _logger.LogInformation("Setup completed successfully");
                    return (true, null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Setup failed with error: {Message}", ex.Message);
                    return (false, $"Setup failed: {ex.Message}");
                }
            }
            finally
            {
                _semaphore.Release();
                _logger.LogInformation("Setup lock released");
            }
        }

        private string GenerateSecureSecret(int length = 32)
        {
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                var bytes = new byte[length];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
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