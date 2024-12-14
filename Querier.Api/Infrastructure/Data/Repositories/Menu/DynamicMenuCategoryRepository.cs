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

        public async Task<List<DynamicMenuCategory>> GetAllAsync()
        {
            return await _context.MenuCategories
                .Include(x => x.Translations)
                .ToListAsync();
        }

        public async Task<DynamicMenuCategory> GetByIdAsync(int id)
        {
            return await _context.MenuCategories
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<DynamicMenuCategory> CreateAsync(DynamicMenuCategory category)
        {
            await _context.MenuCategories.AddAsync(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<DynamicMenuCategory> UpdateAsync(DynamicMenuCategory category)
        {
            _context.MenuCategories.Update(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await _context.MenuCategories.FindAsync(id);
            if (category == null) return false;

            _context.MenuCategories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 