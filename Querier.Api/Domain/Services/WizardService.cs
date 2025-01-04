using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using Querier.Api.Application.DTOs.Requests.Setup;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Common.Metadata;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Domain.Services
{
    public class WizardService : IWizardService, IDisposable
    {
        private readonly UserManager<ApiUser> _userManager;
        private readonly RoleManager<ApiRole> _roleManager;
        private readonly ISettingService _settingService;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger<WizardService> _logger;
        private bool _disposed = false;

        public WizardService(
            UserManager<ApiUser> userManager,
            RoleManager<ApiRole> roleManager,
            ISettingService settingService,
            IDbContextFactory<ApiDbContext> contextFactory,
            ILogger<WizardService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _settingService = settingService;
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<(bool Success, string Error)> SetupAsync(SetupRequest request)
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
                    var jwtSettings = new[]
                    {
                        new QSetting { Name = "JwtSecret", Value = jwtSecret, Description = "JWT authentication secret key", Type = "string" },
                        new QSetting { Name = "JwtIssuer", Value = "QuerierApi", Description = "JWT issuer", Type = "string" },
                        new QSetting { Name = "JwtAudience", Value = "QuerierClient", Description = "JWT audience", Type = "string" },
                        new QSetting { Name = "JwtExpiryInMinutes", Value = "60", Description = "JWT token expiry in minutes", Type = "integer" }
                    };
                    await context.QSettings.AddRangeAsync(jwtSettings);

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
                    var smtpSettings = new[]
                    {
                        new QSetting { Name = "api:smtp:host", Value = request.Smtp.Host },
                        new QSetting { Name = "api:smtp:port", Value = request.Smtp.Port.ToString() },
                        new QSetting { Name = "api:smtp:username", Value = request.Smtp.Username },
                        new QSetting { Name = "api:smtp:password", Value = request.Smtp.Password },
                        new QSetting { Name = "api:smtp:useSSL", Value = request.Smtp.useSSL.ToString() },
                        new QSetting { Name = "api:smtp:senderEmail", Value = request.Smtp.SenderEmail },
                        new QSetting { Name = "api:smtp:senderName", Value = request.Smtp.SenderName },
                        new QSetting { Name = "api:smtp:requiresAuth", Value = request.Smtp.RequireAuth.ToString() },
                    };

                    await context.QSettings.AddRangeAsync(smtpSettings);

                    var isConfiguredSetting = await context.QSettings
                        .FirstOrDefaultAsync(s => s.Name == "api:isConfigured");

                    if (isConfiguredSetting != null)
                    {
                        _logger.LogInformation("Updating existing isConfigured setting");
                        isConfiguredSetting.Value = "true";
                        context.QSettings.Update(isConfiguredSetting);
                    }
                    else
                    {
                        _logger.LogInformation("Creating new isConfigured setting");
                        await context.QSettings.AddAsync(new QSetting
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