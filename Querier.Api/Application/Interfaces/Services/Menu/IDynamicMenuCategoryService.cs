using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Menu.Requests;
using Querier.Api.Application.DTOs.Menu.Responses;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicMenuCategoryService
    {
        Task<List<MenuCategoryResponse>> GetAllAsync();
        Task<MenuCategoryResponse> GetByIdAsync(int id);
        Task<MenuCategoryResponse> CreateAsync(CreateMenuCategoryRequest request);
        Task<MenuCategoryResponse> UpdateAsync(int id, CreateMenuCategoryRequest request);
        Task<bool> DeleteAsync(int id);
    }
} 