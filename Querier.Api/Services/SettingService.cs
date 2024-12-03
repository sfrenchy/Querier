using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Models;
using Querier.Api.Models.Common;

namespace Querier.Api.Services
{
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
            try
            {
                var setting = await _context.QSettings.FirstOrDefaultAsync(s => s.Name == "isConfigured");
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
            try
            {
                var setting = await _context.QSettings.FirstOrDefaultAsync(s => s.Name == name);
                if (setting == null) return default(T);
                if (typeof(T) == typeof(bool)) return (T)(object)(setting.Value.ToLower() == "true");
                return (T)Convert.ChangeType(setting.Value, typeof(T));
            }
            catch (Exception)
            {
                return default(T);
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
    } 
} 