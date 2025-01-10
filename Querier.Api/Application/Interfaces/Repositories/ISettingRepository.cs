using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Common.Metadata;

namespace Querier.Api.Application.Interfaces.Repositories
{
    public interface ISettingRepository
    {
        public Task<IEnumerable<Setting>> ListAsync();
        public Task<Setting> UpdateAsync(Setting entity);
        public Task<Setting> GetByIdAsync(int settingId);
        Task AddAsync(Setting newSetting);  
    }
}