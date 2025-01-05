using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Metadata;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Domain.Services
{

    public class SettingService : ISettingService
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<SettingService> _logger;

        public SettingService(ApiDbContext context, ILogger<SettingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<QSetting> GetSettings()
        {
            return await _context.QSettings.FirstOrDefaultAsync();
        }

        public async Task<QSetting> UpdateSetting(QSetting setting)
        {
            _context.QSettings.Update(setting);
            await _context.SaveChangesAsync();
            return setting;
        }

        public async Task<QSetting> Configure(QSetting setting)
        {
            return await UpdateSetting(setting);
        }

        public async Task<bool> GetIsConfigured()
        {
            try
            {
                var setting = await _context.QSettings.FirstOrDefaultAsync(s => s.Name == "api:isConfigured");
                if (setting == null) return false;
                return setting.Value.ToLower() == "true";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<T> GetSettingValue<T>(string name)
        {
            return await GetSettingValue<T>(name, default);
        }

        public async Task<T> GetSettingValue<T>(string name, T defaultValue)
        {
            try
            {
                var setting = await _context.QSettings.FirstOrDefaultAsync(s => s.Name == name);
                if (setting == null) return defaultValue;
                if (typeof(T) == typeof(bool)) return (T)(object)(setting.Value.ToLower() == "true");
                return (T)Convert.ChangeType(setting.Value, typeof(T));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public async Task<QSetting> CreateSetting(string name, string value)
        {
            var setting = new QSetting
            {
                Name = name,
                Value = value
            };

            _context.QSettings.Add(setting);
            await _context.SaveChangesAsync();

            return setting;
        }

        public async Task<string> GetSettingValue(string name, string defaultValue = null)
        {
            try
            {
                var setting = await _context.QSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Name == name);

                if (setting == null && defaultValue != null)
                {
                    // Créer le paramètre avec la valeur par défaut
                    setting = await CreateSetting(name, defaultValue);
                    return setting.Value;
                }

                return setting?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting setting value for {name}");
                return null;
            }
        }

        public async Task<QSetting> UpdateSettingIfExists(string name, string value)
        {
            var setting = await _context.QSettings.FirstOrDefaultAsync(s => s.Name == name);
            if (setting != null)
            {
                setting.Value = value;
                await _context.SaveChangesAsync();
            }
            else
            {
                setting = new QSetting { Name = name, Value = value };
                _context.QSettings.Add(setting);
                await _context.SaveChangesAsync();
            }
            return setting;
        }

        public async Task UpdateSettings(Dictionary<string, string> settings)
        {
            foreach (var (name, value) in settings)
            {
                await UpdateSettingIfExists(name, value);
            }
        }
    }
}