using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Common.Metadata;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface ISettingService
    {
        Task<Setting> GetSettings();
        Task<Setting> UpdateSetting(Setting setting);
        Task<Setting> Configure(Setting setting);
        Task<bool> GetIsConfigured();
        Task<T> GetSettingValue<T>(string name);
        Task<T> GetSettingValue<T>(string name, T defaultValue);
        Task<Setting> CreateSetting(string name, string value);
        Task<string> GetSettingValue(string name, string defaultValue = null);
        Task<Setting> UpdateSettingIfExists(string name, string value);
        Task UpdateSettings(Dictionary<string, string> settings);
    }


}