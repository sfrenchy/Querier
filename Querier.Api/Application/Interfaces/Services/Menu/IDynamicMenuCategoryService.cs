using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Menu.Requests;
using Querier.Api.Application.DTOs.Menu.Responses;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicMenuCategoryService
    {
        Task<List<DynamicMenuCategoryResponse>> GetAllAsync();
        Task<DynamicMenuCategoryResponse> GetByIdAsync(int id);
        Task<DynamicMenuCategoryResponse> CreateAsync(CreateDynamicMenuCategoryRequest request);
        Task<DynamicMenuCategoryResponse> UpdateAsync(int id, CreateDynamicMenuCategoryRequest request);
        Task<bool> DeleteAsync(int id);
    }
} 