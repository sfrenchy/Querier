using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface ISettingService
    {
        Task<IEnumerable<SettingDto>> GetSettingsAsync();
        Task<SettingDto> CreateSettingAsync(SettingDto dto);
        Task<SettingDto> UpdateSettingAsync(SettingDto setting);
        Task<bool> GetApiIsConfiguredAsync();
        Task<T> GetSettingValueAsync<T>(string name);
        Task<T> GetSettingValueAsync<T>(string name, T defaultValue);
        Task<T> GetSettingValueIfExistsAsync<T>(string name, T defaultValue, string description);
        Task UpdateSettingIfExistsAsync<T>(string name, T value);
        Task UpdateSettings(Dictionary<string, dynamic> settings);
    }


}