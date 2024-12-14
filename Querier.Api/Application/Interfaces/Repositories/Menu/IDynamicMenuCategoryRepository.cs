using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.Interfaces.Repositories.Menu
{
    public interface IDynamicMenuCategoryRepository
    {
        Task<List<MenuCategory>> GetAllAsync();
        Task<MenuCategory> GetByIdAsync(int id);
        Task<MenuCategory> CreateAsync(MenuCategory category);
        Task<MenuCategory> UpdateAsync(MenuCategory category);
        Task<bool> DeleteAsync(int id);
    }
} 