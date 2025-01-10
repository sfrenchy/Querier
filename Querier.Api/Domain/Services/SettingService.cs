using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Common.Metadata;

namespace Querier.Api.Domain.Services
{

    public class SettingService(ISettingRepository settingRepository, ILogger<SettingService> logger)
        : ISettingService
    {
        private readonly ISettingRepository _settingRepository = settingRepository;

        public async Task<IEnumerable<SettingDto>> GetSettingsAsync()
        {
            return (await _settingRepository.ListAsync()).Select(SettingDto.FromEntity);
        }

        public async Task<SettingDto> UpdateSettingAsync(SettingDto setting)
        {
            Setting entity = await _settingRepository.GetByIdAsync(setting.Id);
            return SettingDto.FromEntity(await _settingRepository.UpdateAsync(entity));
        }

        public async Task<bool> GetApiIsConfiguredAsync()
        {
            try
            {
                var setting = (await _settingRepository.ListAsync()).FirstOrDefault(s => s.Name == "api:isConfigured");
                if (setting == null) return false;
                return setting.Value.ToLower() == "true";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<T> GetSettingValueAsync<T>(string name)
        {
            return await GetSettingValueAsync<T>(name, default);
        }

        public async Task<T> GetSettingValueAsync<T>(string name, T defaultValue)
        {
            try
            {
                var setting = (await _settingRepository.ListAsync()).FirstOrDefault(s => s.Name == name);
                if (setting == null) return defaultValue;
                if (typeof(T) == typeof(bool)) return (T)(object)(setting.Value.ToLower() == "true");
                return (T)Convert.ChangeType(setting.Value, typeof(T));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        public async Task<SettingDto> CreateSettingAsync(SettingDto dto)
        {
            Setting newSetting = new Setting();
            newSetting.Name = dto.Name;
            newSetting.Value = dto.Value;
            newSetting.Description = dto.Description;
            newSetting.Type = dto.Type.ToString();
            newSetting.Description = dto.Description;

            await _settingRepository.AddAsync(newSetting);

            return SettingDto.FromEntity(newSetting);
        }

        public async Task<T> GetSettingValueIfExistsAsync<T>(string name, T defaultValue, string description = "")
        {
            var setting = (await _settingRepository.ListAsync()).FirstOrDefault(s => s.Name == name);

            if (setting == null)
            {
                Setting newSetting = new Setting();
                newSetting.Name = name;
                newSetting.Value = defaultValue != null ? defaultValue.ToString() : null;
                newSetting.Description = description;
                newSetting.Type = typeof(T).ToString();
                
                await _settingRepository.AddAsync(newSetting);
                return (T)Convert.ChangeType(newSetting.Value, typeof(T));
            }

            return (T)Convert.ChangeType(setting.Value, typeof(T));
        }

        public async Task UpdateSettingIfExistsAsync<T>(string name, T value, string description = "")
        {
            var setting = (await _settingRepository.ListAsync()).FirstOrDefault(s => s.Name == name);
            if (setting != null)
            {
                if (typeof(T).ToString() != setting.Type)
                    throw new Exception($"Type mismatch, the setting exists with type {setting.Type}");
                
                setting.Value = value.ToString();
                await _settingRepository.UpdateAsync(setting);
            }
            else
            {
                setting = new Setting
                {
                    Name = name, 
                    Value = value.ToString(),
                    Description = description,
                    Type = typeof(T).ToString()
                };
                await _settingRepository.AddAsync(setting);
            }
        }

        public async Task UpdateSettings(Dictionary<string, string> settings)
        {
            foreach (var (name, value) in settings)
            {
                await UpdateSettingIfExistsAsync(name, value);
            }
        }
    }
}