using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories.Menu
{
    public class DynamicRowRepository : IDynamicRowRepository
    {
        private readonly ApiDbContext _context;

        public DynamicRowRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<DynamicRow> GetByIdAsync(int id)
        {
            return await _context.DynamicRows
                .Include(r => r.Cards)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<DynamicRow>> GetByPageIdAsync(int pageId)
        {
            return await _context.DynamicRows
                .Include(r => r.Cards)
                .Where(r => r.PageId == pageId)
                .OrderBy(r => r.Order)
                .ToListAsync();
        }

        public async Task<DynamicRow> CreateAsync(DynamicRow row)
        {
            await _context.DynamicRows.AddAsync(row);
            await _context.SaveChangesAsync();
            return row;
        }

        public async Task<DynamicRow> UpdateAsync(DynamicRow row)
        {
            _context.DynamicRows.Update(row);
            await _context.SaveChangesAsync();
            return row;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var row = await GetByIdAsync(id);
            if (row == null) return false;

            _context.DynamicRows.Remove(row);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetMaxOrderInPageAsync(int pageId)
        {
            return await _context.DynamicRows
                .Where(r => r.PageId == pageId)
                .MaxAsync(r => (int?)r.Order) ?? 0;
        }
    }
} 