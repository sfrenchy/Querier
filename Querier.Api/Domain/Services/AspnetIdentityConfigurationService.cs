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

            options.SignIn.RequireConfirmedAccount = await settingService.GetSettingValue("RequireConfirmedAccount", true);
            options.SignIn.RequireConfirmedEmail = await settingService.GetSettingValue("RequireConfirmedEmail", true);
            
            options.Password.RequireDigit = await settingService.GetSettingValue("PasswordRequireDigit", true);
            options.Password.RequireLowercase = await settingService.GetSettingValue("PasswordRequireLowercase", true);
            options.Password.RequireNonAlphanumeric = await settingService.GetSettingValue("PasswordRequireNonAlphanumeric", true);
            options.Password.RequireUppercase = await settingService.GetSettingValue("PasswordRequireUppercase", true);
            options.Password.RequiredLength = await settingService.GetSettingValue("PasswordRequiredLength", 12);
            options.Password.RequiredUniqueChars = await settingService.GetSettingValue("PasswordRequiredUniqueChars", 1);
        }

        public async Task ConfigureTokenProviderOptions()
        {
            var emailConfirmationOptions = confirmationOptions.CurrentValue;
            var days = await settingService.GetSettingValue("EmailConfirmationTokenLifespanDays", 2);
            emailConfirmationOptions.TokenLifespan = TimeSpan.FromDays(days);

            var dataProtectionOptions = protectionOptions.CurrentValue;
            var minutes = await settingService.GetSettingValue("DataProtectionTokenLifespanMinutes", 15);
            dataProtectionOptions.TokenLifespan = TimeSpan.FromMinutes(minutes);
        }
    }
} 