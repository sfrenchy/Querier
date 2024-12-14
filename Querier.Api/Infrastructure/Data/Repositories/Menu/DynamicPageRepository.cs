using Microsoft.EntityFrameworkCore;
using Querier.Api.Infrastructure.Data.Context;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Data.Repositories.Menu
{
    public class DynamicPageRepository : IDynamicPageRepository
    {
        private readonly ApiDbContext _context;

        public DynamicPageRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<DynamicPage> GetByIdAsync(int id)
        {
            return await _context.DynamicPages.FindAsync(id);
        }

        public async Task<IEnumerable<DynamicPage>> GetAllAsync()
        {
            return await _context.DynamicPages.ToListAsync();
        }

        public async Task<DynamicPage> CreateAsync(DynamicPage page)
        {
            _context.DynamicPages.Add(page);
            await _context.SaveChangesAsync();
            return page;
        }

        public async Task<DynamicPage> UpdateAsync(int id, DynamicPage page)
        {
            var existingPage = await GetByIdAsync(id);
            if (existingPage == null) return null;

            _context.Entry(existingPage).CurrentValues.SetValues(page);
            await _context.SaveChangesAsync();
            return existingPage;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var page = await GetByIdAsync(id);
            if (page == null) return false;

            _context.DynamicPages.Remove(page);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}