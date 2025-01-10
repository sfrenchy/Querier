using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class MenuRepository(ApiDbContext context) : IMenuRepository
    {
        public async Task<List<Domain.Entities.Menu.Menu>> GetAllAsync()
        {
            return await context.Menus
                .Include(x => x.Translations)
                .ToListAsync();
        }

        public async Task<Domain.Entities.Menu.Menu> GetByIdAsync(int id)
        {
            return await context.Menus
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Domain.Entities.Menu.Menu> CreateAsync(Domain.Entities.Menu.Menu category)
        {
            await context.Menus.AddAsync(category);
            await context.SaveChangesAsync();
            return category;
        }

        public async Task<Domain.Entities.Menu.Menu> UpdateAsync(Domain.Entities.Menu.Menu category)
        {
            context.Menus.Update(category);
            await context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var category = await context.Menus.FindAsync(id);
            if (category == null) return false;

            context.Menus.Remove(category);
            await context.SaveChangesAsync();
            return true;
        }
    }
} 