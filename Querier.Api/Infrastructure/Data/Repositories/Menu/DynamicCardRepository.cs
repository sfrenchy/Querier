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

        public async Task<Card> GetByIdAsync(int id)
        {
            return await _context.Cards
                .FindAsync(id);
        }

        public async Task<IEnumerable<Card>> GetByRowIdAsync(int rowId)
        {
            return await _context.Cards
                .Where(c => c.RowId == rowId)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<Card> CreateAsync(Card card)
        {
            await _context.Cards.AddAsync(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public async Task<Card> UpdateAsync(Card card)
        {
            _context.Cards.Update(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var card = await GetByIdAsync(id);
            if (card == null) return false;

            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetMaxOrderInRowAsync(int rowId)
        {
            return await _context.Cards
                .Where(c => c.RowId == rowId)
                .MaxAsync(c => (int?)c.Order) ?? 0;
        }
    }
} 