using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.Interfaces.Repositories.Menu
{
    public interface IDynamicCardRepository
    {
        Task<Card> GetByIdAsync(int id);
        Task<IEnumerable<Card>> GetByRowIdAsync(int rowId);
        Task<Card> CreateAsync(Card card);
        Task<Card> UpdateAsync(Card card);
        Task<bool> DeleteAsync(int id);
        Task<int> GetMaxOrderInRowAsync(int rowId);
    }
} 