using Microsoft.EntityFrameworkCore;
using Querier.Api.Infrastructure.Data.Context;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Data.Repositories.Menu
{
    public class PageRepository : IPageRepository
    {
        private readonly ApiDbContext _context;

        public PageRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<Page> GetByIdAsync(int id)
        {
            return await _context.Pages.FindAsync(id);
        }

        public async Task<IEnumerable<Page>> GetAllAsync()
        {
            return await _context.Pages.ToListAsync();
        }

        public async Task<Page> CreateAsync(Page page)
        {
            _context.Pages.Add(page);
            await _context.SaveChangesAsync();
            return page;
        }

        public async Task<Page> UpdateAsync(int id, Page page)
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

            _context.Pages.Remove(page);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}