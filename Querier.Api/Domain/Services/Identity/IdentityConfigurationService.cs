using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Security.TokenProviders;

namespace Querier.Api.Domain.Services.Identity
{
    public class IdentityConfigurationService : IIdentityConfigurationService
    {
        private readonly ISettingService _settingService;
        private readonly IOptionsMonitor<IdentityOptions> _identityOptions;
        private readonly IOptionsMonitor<EmailConfirmationTokenProviderOptions> _emailConfirmationOptions;
        private readonly IOptionsMonitor<DataProtectionTokenProviderOptions> _dataProtectionOptions;

        public IdentityConfigurationService(
            ISettingService settingService,
            IOptionsMonitor<IdentityOptions> identityOptions,
            IOptionsMonitor<EmailConfirmationTokenProviderOptions> emailConfirmationOptions,
            IOptionsMonitor<DataProtectionTokenProviderOptions> dataProtectionOptions)
        {
            _settingService = settingService;
            _identityOptions = identityOptions;
            _emailConfirmationOptions = emailConfirmationOptions;
            _dataProtectionOptions = dataProtectionOptions;
        }

        public async Task ConfigureIdentityOptions()
        {
            var options = _identityOptions.CurrentValue;

            options.SignIn.RequireConfirmedAccount = await _settingService.GetSettingValue("RequireConfirmedAccount", true);
            options.SignIn.RequireConfirmedEmail = await _settingService.GetSettingValue("RequireConfirmedEmail", true);
            
            options.Password.RequireDigit = await _settingService.GetSettingValue("PasswordRequireDigit", true);
            options.Password.RequireLowercase = await _settingService.GetSettingValue("PasswordRequireLowercase", true);
            options.Password.RequireNonAlphanumeric = await _settingService.GetSettingValue("PasswordRequireNonAlphanumeric", true);
            options.Password.RequireUppercase = await _settingService.GetSettingValue("PasswordRequireUppercase", true);
            options.Password.RequiredLength = await _settingService.GetSettingValue("PasswordRequiredLength", 12);
            options.Password.RequiredUniqueChars = await _settingService.GetSettingValue("PasswordRequiredUniqueChars", 1);
        }

        public async Task ConfigureTokenProviderOptions()
        {
            var emailConfirmationOptions = _emailConfirmationOptions.CurrentValue;
            var days = await _settingService.GetSettingValue("EmailConfirmationTokenLifespanDays", 2);
            emailConfirmationOptions.TokenLifespan = TimeSpan.FromDays(days);

            var dataProtectionOptions = _dataProtectionOptions.CurrentValue;
            var minutes = await _settingService.GetSettingValue("DataProtectionTokenLifespanMinutes", 15);
            dataProtectionOptions.TokenLifespan = TimeSpan.FromMinutes(minutes);
        }
    }
} 