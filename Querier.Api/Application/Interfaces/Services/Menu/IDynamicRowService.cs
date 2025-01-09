using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicRowService
    {
        Task<RowDto> GetByIdAsync(int id);
        Task<IEnumerable<RowDto>> GetByPageIdAsync(int pageId);
        Task<RowDto> CreateAsync(int pageId, RowCreateDto request);
        Task<RowDto> UpdateAsync(int id, RowCreateDto request);
        Task<bool> DeleteAsync(int id);
        Task<bool> ReorderAsync(int pageId, List<int> rowIds);
    }
} 