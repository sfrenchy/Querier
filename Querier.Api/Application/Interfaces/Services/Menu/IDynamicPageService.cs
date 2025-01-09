using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicPageService
    {
        Task<PageDto> GetByIdAsync(int id);
        Task<IEnumerable<PageDto>> GetAllAsync();
        Task<PageDto> CreateAsync(PageCreateDto request);
        Task<PageDto> UpdateAsync(int id, PageUpdateDto request);
        Task<bool> DeleteAsync(int id);
    }
} 