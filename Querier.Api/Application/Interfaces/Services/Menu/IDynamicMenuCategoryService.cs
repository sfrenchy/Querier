using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicMenuCategoryService
    {
        Task<List<MenuDto>> GetAllAsync();
        Task<MenuDto> GetByIdAsync(int id);
        Task<MenuDto> CreateAsync(MenuCreateDto request);
        Task<MenuDto> UpdateAsync(int id, MenuCreateDto request);
        Task<bool> DeleteAsync(int id);
    }
} 