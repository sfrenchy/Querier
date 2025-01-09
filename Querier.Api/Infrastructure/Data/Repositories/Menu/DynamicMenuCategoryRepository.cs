using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories.Menu
{
    public class DynamicMenuCategoryRepository : IDynamicMenuCategoryRepository
    {
        private readonly ApiDbContext _context;

        public DynamicMenuCategoryRepository(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<List<Domain.Entities.Menu.Menu>> GetAllAsync()
        {
            return await _context.Menus
                .Include(x => x.Translations)
                .ToListAsync();
        }

        public async Task<Domain.Entities.Menu.Menu> GetByIdAsync(int id)
        {
            return await _context.Menus
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Domain.Entities.Menu.Menu> CreateAsync(Domain.Entities.Menu.Menu category)
        {
            await _context.Menus.AddAsync(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Domain.Entities.Menu.Menu> UpdateAsync(Domain.Entities.Menu.Menu category)
        {
            _context.Menus.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.Menus.FindAsync(id);
            if (category == null) return false;

            _context.Menus.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 