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

        public async Task<Row> GetByIdAsync(int id)
        {
            return await _context.Rows
                .Include(r => r.Cards)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Row>> GetByPageIdAsync(int pageId)
        {
            return await _context.Rows
                .Include(r => r.Cards)
                .Where(r => r.PageId == pageId)
                .OrderBy(r => r.Order)
                .ToListAsync();
        }

        public async Task<Row> CreateAsync(Row row)
        {
            await _context.Rows.AddAsync(row);
            await _context.SaveChangesAsync();
            return row;
        }

        public async Task<Row> UpdateAsync(Row row)
        {
            _context.Rows.Update(row);
            await _context.SaveChangesAsync();
            return row;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var row = await GetByIdAsync(id);
            if (row == null) return false;

            _context.Rows.Remove(row);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetMaxOrderInPageAsync(int pageId)
        {
            return await _context.Rows
                .Where(r => r.PageId == pageId)
                .MaxAsync(r => (int?)r.Order) ?? 0;
        }
    }
} 