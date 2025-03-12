using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Common.Extensions;
using Querier.Api.Domain.Entities;

namespace Querier.Api.Domain.Services
{
    public class SettingService
        : ISettingService
    {
        private readonly ILogger<SettingService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private ISettingRepository _settingRepository;
        public SettingService(ILogger<SettingService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private ISettingRepository settingRepository
        {
            get
            {
                if (_settingRepository == null)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        _settingRepository = scope.ServiceProvider.GetRequiredService<ISettingRepository>();
                    }
                }
                return _settingRepository;
            }
        }

        public async Task<IEnumerable<SettingDto>> GetSettingsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all settings");
                var settings = await settingRepository.ListAsync();
                var enumerable = settings.ToList();
                var dtos = enumerable.Select(SettingDto.FromEntity);
                _logger.LogInformation("Successfully retrieved {Count} settings", enumerable.Count());
                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve settings");
                throw;
            }
        }

        public async Task<SettingDto> UpdateSettingAsync(SettingDto setting)
        {
            try
            {
                _logger.LogInformation("Attempting to update setting: {Name}", setting.Name);

                if (setting == null)
                {
                    _logger.LogError("UpdateSettingAsync called with null setting");
                    throw new ArgumentNullException(nameof(setting));
                }

                var entity = await settingRepository.GetByIdAsync(setting.Id);
                if (entity == null)
                {
                    _logger.LogWarning("Setting not found with ID: {Id}", setting.Id);
                    throw new KeyNotFoundException($"Setting with ID {setting.Id} not found");
                }

                var updatedEntity = await settingRepository.UpdateAsync(entity);
                _logger.LogInformation("Successfully updated setting: {Name}", setting.Name);
                return SettingDto.FromEntity(updatedEntity);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not KeyNotFoundException)
            {
                _logger.LogError(ex, "Failed to update setting: {Name}", setting?.Name);
                throw;
            }
        }

        public async Task<bool> GetApiIsConfiguredAsync()
        {
            try
            {
                _logger.LogInformation("Checking if API is configured");
                var settings = await settingRepository.ListAsync();
                var setting = settings.FirstOrDefault(s => s.Name == "api:isConfigured");
                
                if (setting == null)
                {
                    _logger.LogInformation("API configuration setting not found, returning false");
                    return false;
                }

                var isConfigured = setting.Value.ToLower() == "true";
                _logger.LogInformation("API configuration status: {IsConfigured}", isConfigured);
                return isConfigured;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API configuration status");
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
                _logger.LogDebug("Retrieving setting value: {Name}", name);

                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("GetSettingValueAsync called with null or empty name");
                    return defaultValue;
                }

                var settings = await settingRepository.ListAsync();
                var setting = settings.FirstOrDefault(s => s.Name == name);

                if (setting == null)
                {
                    _logger.LogDebug("Setting {Name} not found, returning default value", name);
                    return defaultValue;
                }

                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)(setting.Value.ToLower() == "true");
                    }

                    var convertedValue = (T)Convert.ChangeType(setting.Value, typeof(T));
                    _logger.LogDebug("Successfully retrieved setting {Name} with value {Value}", name, convertedValue);
                    return convertedValue;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert setting {Name} to type {Type}, returning default value", 
                        name, typeof(T).Name);
                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving setting {Name}", name);
                return defaultValue;
            }
        }

        public async Task<SettingDto> CreateSettingAsync(SettingDto dto)
        {
            try
            {
                _logger.LogInformation("Attempting to create new setting: {Name}", dto.Name);

                if (dto == null)
                {
                    _logger.LogError("CreateSettingAsync called with null DTO");
                    throw new ArgumentNullException(nameof(dto));
                }

                if (string.IsNullOrEmpty(dto.Name))
                {
                    _logger.LogError("Cannot create setting with null or empty name");
                    throw new ArgumentException("Setting name cannot be null or empty", nameof(dto.Name));
                }

                var newSetting = new Setting
                {
                    Name = dto.Name,
                    Value = dto.Value,
                    Description = dto.Description,
                    Type = dto.Type.ToString()
                };

                await settingRepository.AddAsync(newSetting);
                _logger.LogInformation("Successfully created setting: {Name}", dto.Name);

                return SettingDto.FromEntity(newSetting);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not ArgumentException)
            {
                _logger.LogError(ex, "Failed to create setting: {Name}", dto?.Name);
                throw;
            }
        }

        public async Task<T> GetSettingValueIfExistsAsync<T>(string name, T defaultValue, string description = "")
        {
            try
            {
                _logger.LogDebug("Retrieving or creating setting: {Name}", name);

                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("GetSettingValueIfExistsAsync called with null or empty name");
                    throw new ArgumentException("Setting name cannot be null or empty", nameof(name));
                }

                var settings = await settingRepository.ListAsync();
                var setting = settings.FirstOrDefault(s => s.Name == name);

                if (setting == null)
                {
                    _logger.LogInformation("Setting {Name} not found, creating with default value", name);
                    
                    var newSetting = new Setting
                    {
                        Name = name,
                        Value = defaultValue?.ToString(),
                        Description = description,
                        Type = typeof(T).ToString()
                    };

                    await settingRepository.AddAsync(newSetting);
                    return (T)Convert.ChangeType(newSetting.Value, typeof(T));
                }

                var convertedValue = (T)Convert.ChangeType(setting.Value, typeof(T));
                _logger.LogDebug("Successfully retrieved existing setting {Name} with value {Value}", name, convertedValue);
                return convertedValue;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, "Failed to retrieve or create setting: {Name}", name);
                throw;
            }
        }

        public async Task UpdateSettingIfExistsAsync<T>(string name, T value)
        {
            try
            {
                _logger.LogInformation("Attempting to update or create setting: {Name}", name);

                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogError("UpdateSettingIfExistsAsync called with null or empty name");
                    throw new ArgumentException("Setting name cannot be null or empty", nameof(name));
                }

                var settings = await settingRepository.ListAsync();
                var setting = settings.FirstOrDefault(s => s.Name == name);

                if (setting != null)
                {
                    if (typeof(T).ToString() != setting.Type)
                    {
                        var error = $"Type mismatch for setting {name}, existing type: {setting.Type}, new type: {typeof(T)}";
                        _logger.LogError(error);
                        throw new InvalidOperationException(error);
                    }

                    setting.Value = value?.ToString();
                    await settingRepository.UpdateAsync(setting);
                    _logger.LogInformation("Successfully updated existing setting: {Name}", name);
                }
                else
                {
                    setting = new Setting
                    {
                        Name = name,
                        Value = value?.ToString(),
                        Type = typeof(T).ToString()
                    };
                    await settingRepository.AddAsync(setting);
                    _logger.LogInformation("Successfully created new setting: {Name}", name);
                }
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to update or create setting: {Name}", name);
                throw;
            }
        }

        public async Task UpdateSettings(Dictionary<string, dynamic> settings)
        {
            try
            {
                _logger.LogInformation("Updating multiple settings. Count: {Count}", settings.Count);

                if (settings == null)
                {
                    _logger.LogError("UpdateSettings called with null dictionary");
                    throw new ArgumentNullException(nameof(settings));
                }

                foreach (var (name, value) in settings)
                {
                    try
                    {
                        await UpdateSettingIfExistsAsync(name, value.Item1);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update setting {Name}, continuing with remaining settings", name);
                    }
                }

                _logger.LogInformation("Completed updating multiple settings");
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                _logger.LogError(ex, "Failed to update multiple settings");
                throw;
            }
        }
    }
}