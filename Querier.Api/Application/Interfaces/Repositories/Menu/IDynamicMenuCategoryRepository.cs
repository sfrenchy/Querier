using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.Interfaces.Repositories.Menu
{
    public interface IDynamicMenuCategoryRepository
    {
        Task<List<DynamicMenuCategory>> GetAllAsync();
        Task<DynamicMenuCategory> GetByIdAsync(int id);
        Task<DynamicMenuCategory> CreateAsync(DynamicMenuCategory category);
        Task<DynamicMenuCategory> UpdateAsync(DynamicMenuCategory category);
        Task<bool> DeleteAsync(int id);
    }
} 