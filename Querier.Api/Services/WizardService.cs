using Microsoft.AspNetCore.Identity;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq;

namespace Querier.Api.Services
{
    public class WizardService : IWizardService
    {
        private readonly UserManager<ApiUser> _userManager;
        private readonly RoleManager<ApiRole> _roleManager;
        private readonly ISettingService _settingService;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger<WizardService> _logger;

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
                await _semaphore.WaitAsync();
                
                try
                {
                    if (!await _roleManager.RoleExistsAsync("Admin"))
                    {
                        _logger.LogInformation("Creating Admin role...");
                        var createRoleResult = await _roleManager.CreateAsync(new ApiRole { Name = "Admin" });
                        if (!createRoleResult.Succeeded)
                        {
                            return (false, "Failed to create Admin role");
                        }
                    }

                    _logger.LogInformation("Creating admin user with email: {Email}", request.Admin.Email);
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
                        _logger.LogError("Failed to assign admin role");
                        return (false, "Failed to assign admin role");
                    }

                    using var context = await _contextFactory.CreateDbContextAsync();
                    _logger.LogInformation("Configuring SMTP settings...");
                    var smtpSettings = new[]
                    {
                        new QSetting { Name = "smtp:host", Value = request.Smtp.Host },
                        new QSetting { Name = "smtp:port", Value = request.Smtp.Port.ToString() },
                        new QSetting { Name = "smtp:username", Value = request.Smtp.Username },
                        new QSetting { Name = "smtp:password", Value = request.Smtp.Password },
                        new QSetting { Name = "smtp:useSSL", Value = request.Smtp.UseSSL.ToString() }
                    };

                    await context.QSettings.AddRangeAsync(smtpSettings);

                    var isConfiguredSetting = await context.QSettings
                        .FirstOrDefaultAsync(s => s.Name == "isConfigured");

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
                            Name = "isConfigured", 
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
    }
} 