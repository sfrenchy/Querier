using System.Threading.Tasks;
using Querier.Api.Models.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System;

namespace Querier.Api.Services
{
    public interface ISettingService
    {
        /// <summary>
        /// Get all settings
        /// </summary>
        /// <returns>The settings</returns>
        Task<QSetting> GetSettings();
        /// <summary>
        /// Update a setting
        /// </summary>
        /// <param name="setting"></param>
        /// <returns>The updated setting</returns>
        Task<QSetting> UpdateSetting(QSetting setting);
        /// <summary>
        /// Configure an application setting
        /// </summary>
        /// <param name="setting"></param>
        /// <returns>The updated setting</returns>
        Task<QSetting> Configure(QSetting setting);
        /// <summary>
        /// Get if the application is configured
        /// </summary>
        /// <returns>True if the application is configured, false otherwise</returns>
        Task<bool> GetIsConfigured();
        /// <summary>
        /// Create a new setting
        /// </summary>
        /// <param name="name">The name/key of the setting</param>
        /// <param name="value">The value of the setting</param>
        /// <returns>The created setting</returns>
        Task<QSetting> CreateSetting(string name, string value);
    }

    public class SettingService : ISettingService
    {
        private readonly ApiDbContext _context;

        public SettingService(ApiDbContext context)
        {
            _context = context;
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
            return await GetSettingValue<bool>("isConfigured");
        }

        public async Task<T> GetSettingValue<T>(string name)
        {
            var setting = await _context.QSettings.FirstOrDefaultAsync(s => s.Name == name);
            return (T)Convert.ChangeType(setting.Value, typeof(T));
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
    } 
} 