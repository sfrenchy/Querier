using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class CardRepository(ApiDbContext context) : ICardRepository
    {
        public async Task<Card> GetByIdAsync(int id)
        {
            return await context.Cards
                .FindAsync(id);
        }

        public async Task<IEnumerable<Card>> GetByRowIdAsync(int rowId)
        {
            return await context.Cards
                .Where(c => c.RowId == rowId)
                .OrderBy(c => c.Order)
                .ToListAsync();
        }

        public async Task<Card> CreateAsync(Card card)
        {
            await context.Cards.AddAsync(card);
            await context.SaveChangesAsync();
            return card;
        }

        public async Task<Card> UpdateAsync(Card card)
        {
            context.Cards.Update(card);
            await context.SaveChangesAsync();
            return card;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var card = await GetByIdAsync(id);
            if (card == null) return false;

            context.Cards.Remove(card);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetMaxOrderInRowAsync(int rowId)
        {
            return await context.Cards
                .Where(c => c.RowId == rowId)
                .MaxAsync(c => (int?)c.Order) ?? 0;
        }
    }
} 