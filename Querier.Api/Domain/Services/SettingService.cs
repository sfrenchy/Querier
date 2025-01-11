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
    public class SettingService(
        ISettingRepository settingRepository,
        ILogger<SettingService> logger)
        : ISettingService
    {
        public async Task<IEnumerable<SettingDto>> GetSettingsAsync()
        {
            try
            {
                logger.LogInformation("Retrieving all settings");
                var settings = await settingRepository.ListAsync();
                var enumerable = settings.ToList();
                var dtos = enumerable.Select(SettingDto.FromEntity);
                logger.LogInformation("Successfully retrieved {Count} settings", enumerable.Count());
                return dtos;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve settings");
                throw;
            }
        }

        public async Task<SettingDto> UpdateSettingAsync(SettingDto setting)
        {
            try
            {
                logger.LogInformation("Attempting to update setting: {Name}", setting.Name);

                if (setting == null)
                {
                    logger.LogError("UpdateSettingAsync called with null setting");
                    throw new ArgumentNullException(nameof(setting));
                }

                var entity = await settingRepository.GetByIdAsync(setting.Id);
                if (entity == null)
                {
                    logger.LogWarning("Setting not found with ID: {Id}", setting.Id);
                    throw new KeyNotFoundException($"Setting with ID {setting.Id} not found");
                }

                var updatedEntity = await settingRepository.UpdateAsync(entity);
                logger.LogInformation("Successfully updated setting: {Name}", setting.Name);
                return SettingDto.FromEntity(updatedEntity);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not KeyNotFoundException)
            {
                logger.LogError(ex, "Failed to update setting: {Name}", setting?.Name);
                throw;
            }
        }

        public async Task<bool> GetApiIsConfiguredAsync()
        {
            try
            {
                logger.LogInformation("Checking if API is configured");
                var settings = await settingRepository.ListAsync();
                var setting = settings.FirstOrDefault(s => s.Name == "api:isConfigured");
                
                if (setting == null)
                {
                    logger.LogInformation("API configuration setting not found, returning false");
                    return false;
                }

                var isConfigured = setting.Value.ToLower() == "true";
                logger.LogInformation("API configuration status: {IsConfigured}", isConfigured);
                return isConfigured;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking API configuration status");
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
                logger.LogDebug("Retrieving setting value: {Name}", name);

                if (string.IsNullOrEmpty(name))
                {
                    logger.LogWarning("GetSettingValueAsync called with null or empty name");
                    return defaultValue;
                }

                var settings = await settingRepository.ListAsync();
                var setting = settings.FirstOrDefault(s => s.Name == name);

                if (setting == null)
                {
                    logger.LogDebug("Setting {Name} not found, returning default value", name);
                    return defaultValue;
                }

                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)(setting.Value.ToLower() == "true");
                    }

                    var convertedValue = (T)Convert.ChangeType(setting.Value, typeof(T));
                    logger.LogDebug("Successfully retrieved setting {Name} with value {Value}", name, convertedValue);
                    return convertedValue;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to convert setting {Name} to type {Type}, returning default value", 
                        name, typeof(T).Name);
                    return defaultValue;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error retrieving setting {Name}", name);
                return defaultValue;
            }
        }

        public async Task<SettingDto> CreateSettingAsync(SettingDto dto)
        {
            try
            {
                logger.LogInformation("Attempting to create new setting: {Name}", dto.Name);

                if (dto == null)
                {
                    logger.LogError("CreateSettingAsync called with null DTO");
                    throw new ArgumentNullException(nameof(dto));
                }

                if (string.IsNullOrEmpty(dto.Name))
                {
                    logger.LogError("Cannot create setting with null or empty name");
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
                logger.LogInformation("Successfully created setting: {Name}", dto.Name);

                return SettingDto.FromEntity(newSetting);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not ArgumentException)
            {
                logger.LogError(ex, "Failed to create setting: {Name}", dto?.Name);
                throw;
            }
        }

        public async Task<T> GetSettingValueIfExistsAsync<T>(string name, T defaultValue, string description = "")
        {
            try
            {
                logger.LogDebug("Retrieving or creating setting: {Name}", name);

                if (string.IsNullOrEmpty(name))
                {
                    logger.LogWarning("GetSettingValueIfExistsAsync called with null or empty name");
                    throw new ArgumentException("Setting name cannot be null or empty", nameof(name));
                }

                var settings = await settingRepository.ListAsync();
                var setting = settings.FirstOrDefault(s => s.Name == name);

                if (setting == null)
                {
                    logger.LogInformation("Setting {Name} not found, creating with default value", name);
                    
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
                logger.LogDebug("Successfully retrieved existing setting {Name} with value {Value}", name, convertedValue);
                return convertedValue;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                logger.LogError(ex, "Failed to retrieve or create setting: {Name}", name);
                throw;
            }
        }

        public async Task UpdateSettingIfExistsAsync<T>(string name, T value, string description = "")
        {
            try
            {
                logger.LogInformation("Attempting to update or create setting: {Name}", name);

                if (string.IsNullOrEmpty(name))
                {
                    logger.LogError("UpdateSettingIfExistsAsync called with null or empty name");
                    throw new ArgumentException("Setting name cannot be null or empty", nameof(name));
                }

                var settings = await settingRepository.ListAsync();
                var setting = settings.FirstOrDefault(s => s.Name == name);

                if (setting != null)
                {
                    if (typeof(T).ToString() != setting.Type)
                    {
                        var error = $"Type mismatch for setting {name}, existing type: {setting.Type}, new type: {typeof(T)}";
                        logger.LogError(error);
                        throw new InvalidOperationException(error);
                    }

                    setting.Value = value?.ToString();
                    await settingRepository.UpdateAsync(setting);
                    logger.LogInformation("Successfully updated existing setting: {Name}", name);
                }
                else
                {
                    setting = new Setting
                    {
                        Name = name,
                        Value = value?.ToString(),
                        Description = description,
                        Type = typeof(T).ToString()
                    };
                    await settingRepository.AddAsync(setting);
                    logger.LogInformation("Successfully created new setting: {Name}", name);
                }
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Failed to update or create setting: {Name}", name);
                throw;
            }
        }

        public async Task UpdateSettings(Dictionary<string, string> settings)
        {
            try
            {
                logger.LogInformation("Updating multiple settings. Count: {Count}", settings.Count);

                if (settings == null)
                {
                    logger.LogError("UpdateSettings called with null dictionary");
                    throw new ArgumentNullException(nameof(settings));
                }

                foreach (var (name, value) in settings)
                {
                    try
                    {
                        await UpdateSettingIfExistsAsync(name, value);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to update setting {Name}, continuing with remaining settings", name);
                    }
                }

                logger.LogInformation("Completed updating multiple settings");
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                logger.LogError(ex, "Failed to update multiple settings");
                throw;
            }
        }
    }
}