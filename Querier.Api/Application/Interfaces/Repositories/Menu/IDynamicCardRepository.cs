using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.Interfaces.Repositories.Menu
{
    public interface IDynamicCardRepository
    {
        Task<DynamicCard> GetByIdAsync(int id);
        Task<IEnumerable<DynamicCard>> GetByRowIdAsync(int rowId);
        Task<DynamicCard> CreateAsync(DynamicCard card);
        Task<DynamicCard> UpdateAsync(DynamicCard card);
        Task<bool> DeleteAsync(int id);
        Task<int> GetMaxOrderInRowAsync(int rowId);
    }
} 