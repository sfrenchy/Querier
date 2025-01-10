using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Infrastructure.Security.TokenProviders;

namespace Querier.Api.Domain.Services
{
    public class AspnetIdentityConfigurationService(
        ISettingService settingService,
        IOptionsMonitor<IdentityOptions> identityOptions,
        IOptionsMonitor<EmailConfirmationTokenProviderOptions> confirmationOptions,
        IOptionsMonitor<DataProtectionTokenProviderOptions> protectionOptions)
        : IAspnetIdentityConfigurationService
    {
        public async Task ConfigureIdentityOptions()
        {
            var options = identityOptions.CurrentValue;

            options.SignIn.RequireConfirmedAccount = await settingService.GetSettingValueAsync("RequireConfirmedAccount", true);
            options.SignIn.RequireConfirmedEmail = await settingService.GetSettingValueAsync("RequireConfirmedEmail", true);
            
            options.Password.RequireDigit = await settingService.GetSettingValueAsync("PasswordRequireDigit", true);
            options.Password.RequireLowercase = await settingService.GetSettingValueAsync("PasswordRequireLowercase", true);
            options.Password.RequireNonAlphanumeric = await settingService.GetSettingValueAsync("PasswordRequireNonAlphanumeric", true);
            options.Password.RequireUppercase = await settingService.GetSettingValueAsync("PasswordRequireUppercase", true);
            options.Password.RequiredLength = await settingService.GetSettingValueAsync("PasswordRequiredLength", 12);
            options.Password.RequiredUniqueChars = await settingService.GetSettingValueAsync("PasswordRequiredUniqueChars", 1);
        }

        public async Task ConfigureTokenProviderOptions()
        {
            var emailConfirmationOptions = confirmationOptions.CurrentValue;
            var days = await settingService.GetSettingValueAsync("EmailConfirmationTokenLifespanDays", 2);
            emailConfirmationOptions.TokenLifespan = TimeSpan.FromDays(days);

            var dataProtectionOptions = protectionOptions.CurrentValue;
            var minutes = await settingService.GetSettingValueAsync("DataProtectionTokenLifespanMinutes", 15);
            dataProtectionOptions.TokenLifespan = TimeSpan.FromMinutes(minutes);
        }
    }
} 