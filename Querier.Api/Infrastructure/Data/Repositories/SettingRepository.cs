using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Common.Metadata;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class SettingRepository(IDbContextFactory<ApiDbContext> contextFactory) : ISettingRepository
    {
        public async Task<IEnumerable<Setting>> ListAsync()
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Settings.ToListAsync();
            }
        }

        public async Task<Setting> GetByIdAsync(int settingId)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                return await context.Settings.SingleOrDefaultAsync(setting => setting.Id == settingId);
            }
        }
        
        public async Task<Setting> UpdateAsync(Setting setting)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                context.Settings.Update(setting);
                await context.SaveChangesAsync();
                return setting;
            }
        }

        public async Task AddAsync(Setting setting)
        {
            using (var context = contextFactory.CreateDbContext())
            {
                await context.Settings.AddAsync(setting);
                await context.SaveChangesAsync();
            }
        }
    }
}