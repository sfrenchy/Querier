using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.Interfaces.Repositories.Menu
{
    public interface IDynamicRowRepository
    {
        Task<DynamicRow> GetByIdAsync(int id);
        Task<IEnumerable<DynamicRow>> GetByPageIdAsync(int pageId);
        Task<DynamicRow> CreateAsync(DynamicRow row);
        Task<DynamicRow> UpdateAsync(DynamicRow row);
        Task<bool> DeleteAsync(int id);
        Task<int> GetMaxOrderInPageAsync(int pageId);
    }
} 