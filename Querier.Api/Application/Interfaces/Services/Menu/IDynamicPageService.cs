using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Menu.Requests;
using Querier.Api.Application.DTOs.Requests.Page;
using Querier.Api.Application.DTOs.Responses.Page;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicPageService
    {
        Task<PageResponse> GetByIdAsync(int id);
        Task<IEnumerable<PageResponse>> GetAllAsync();
        Task<PageResponse> CreateAsync(CreatePageRequest request);
        Task<PageResponse> UpdateAsync(int id, UpdateDynamicPageRequest request);
        Task<bool> DeleteAsync(int id);
    }
} 