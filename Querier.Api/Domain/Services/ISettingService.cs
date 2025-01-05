using System.Threading.Tasks;
using System.Collections.Generic;
using Querier.Api.Domain.Common.Metadata;

namespace Querier.Api.Domain.Services
{
    public interface ISettingService
    {
        Task<QSetting> GetSettings();
        Task<QSetting> UpdateSetting(QSetting setting);
        Task<QSetting> Configure(QSetting setting);
        Task<bool> GetIsConfigured();
        Task<T> GetSettingValue<T>(string name);
        Task<T> GetSettingValue<T>(string name, T defaultValue);
        Task<QSetting> CreateSetting(string name, string value);
        Task<string> GetSettingValue(string name, string defaultValue = null);
        Task<QSetting> UpdateSettingIfExists(string name, string value);
        Task UpdateSettings(Dictionary<string, string> settings);
    }


}