using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.Interfaces.Repositories.Menu
{
    public interface IDynamicMenuCategoryRepository
    {
        Task<List<Domain.Entities.Menu.Menu>> GetAllAsync();
        Task<Domain.Entities.Menu.Menu> GetByIdAsync(int id);
        Task<Domain.Entities.Menu.Menu> CreateAsync(Domain.Entities.Menu.Menu category);
        Task<Domain.Entities.Menu.Menu> UpdateAsync(Domain.Entities.Menu.Menu category);
        Task<bool> DeleteAsync(int id);
    }
} 