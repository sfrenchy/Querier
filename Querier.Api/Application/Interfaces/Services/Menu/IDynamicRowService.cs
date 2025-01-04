using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Menu.Requests;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicRowService
    {
        Task<DynamicRowResponse> GetByIdAsync(int id);
        Task<IEnumerable<DynamicRowResponse>> GetByPageIdAsync(int pageId);
        Task<DynamicRowResponse> CreateAsync(int pageId, CreateDynamicRowRequest request);
        Task<DynamicRowResponse> UpdateAsync(int id, CreateDynamicRowRequest request);
        Task<bool> DeleteAsync(int id);
        Task<bool> ReorderAsync(int pageId, List<int> rowIds);
    }
} 