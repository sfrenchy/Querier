using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class PageRepository(ApiDbContext context) : IPageRepository
    {
        public async Task<Page> GetByIdAsync(int id)
        {
            return await context.Pages.FindAsync(id);
        }

        public async Task<IEnumerable<Page>> GetAllAsync()
        {
            return await context.Pages.ToListAsync();
        }

        public async Task<Page> CreateAsync(Page page)
        {
            context.Pages.Add(page);
            await context.SaveChangesAsync();
            return page;
        }

        public async Task<Page> UpdateAsync(int id, Page page)
        {
            var existingPage = await context.Pages
                .Include(p => p.PageTranslations)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (existingPage == null) return null;

            // Mise à jour des propriétés simples
            existingPage.Icon = page.Icon;
            existingPage.Order = page.Order;
            existingPage.IsVisible = page.IsVisible;
            existingPage.Roles = page.Roles;
            existingPage.Route = page.Route;
            existingPage.MenuId = page.MenuId;

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

            await context.SaveChangesAsync();
            return existingPage;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var page = await GetByIdAsync(id);
            if (page == null) return false;

            context.Pages.Remove(page);
            await context.SaveChangesAsync();
            return true;
        }
    }
}