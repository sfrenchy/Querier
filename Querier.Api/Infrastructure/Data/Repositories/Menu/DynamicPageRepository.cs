using Microsoft.EntityFrameworkCore;
using Querier.Api.Infrastructure.Data.Context;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var existingPage = await _context.Pages
                .Include(p => p.PageTranslations)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (existingPage == null) return null;

            // Mise à jour des propriétés simples
            existingPage.Icon = page.Icon;
            existingPage.Order = page.Order;
            existingPage.IsVisible = page.IsVisible;
            existingPage.Roles = page.Roles;
            existingPage.Route = page.Route;
            existingPage.DynamicMenuCategoryId = page.DynamicMenuCategoryId;

            // Mise à jour des traductions
            existingPage.PageTranslations.Clear();
            foreach (var translation in page.PageTranslations)
            {
                existingPage.PageTranslations.Add(new PageTranslation
                {
                    LanguageCode = translation.LanguageCode,
                    Name = translation.Name
                });
            }

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