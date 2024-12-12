using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories.Menu
{
    public class DynamicCardRepository : IDynamicCardRepository
    {
        private readonly ApiDbContext _context;

        public DynamicCardRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<DynamicCard> GetByIdAsync(int id)
        {
            return await _context.DynamicCards
                .FindAsync(id);
        }

        public async Task<IEnumerable<DynamicCard>> GetByRowIdAsync(int rowId)
        {
            return await _context.DynamicCards
                .Where(c => c.DynamicRowId == rowId)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<DynamicCard> CreateAsync(DynamicCard card)
        {
            await _context.DynamicCards.AddAsync(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public async Task<DynamicCard> UpdateAsync(DynamicCard card)
        {
            _context.DynamicCards.Update(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var card = await GetByIdAsync(id);
            if (card == null) return false;

            _context.DynamicCards.Remove(card);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetMaxOrderInRowAsync(int rowId)
        {
            return await _context.DynamicCards
                .Where(c => c.DynamicRowId == rowId)
                .MaxAsync(c => (int?)c.Order) ?? 0;
        }
    }
} 