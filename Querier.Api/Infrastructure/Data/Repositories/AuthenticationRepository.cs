using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class AuthenticationRepository(IDbContextFactory<ApiDbContext> contextFactory) : IAuthenticationRepository
    {
        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            using (var context = await contextFactory.CreateDbContextAsync())
            {
                await context.RefreshTokens.AddAsync(refreshToken);
                await context.SaveChangesAsync();
            }
        }

        public async Task<RefreshToken> GetRefreshTokenAsync(string refreshToken)
        {
            using (var context = await contextFactory.CreateDbContextAsync())
            {
                return await context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
            }
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            using (var context = await contextFactory.CreateDbContextAsync())
            {
                context.RefreshTokens.Update(refreshToken);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteRefreshTokensForUserAsync(string userId)
        {
            using (var context = await contextFactory.CreateDbContextAsync())
            {
                var tokens = context.RefreshTokens.Where(t => t.UserId == userId).ToList();
                context.RefreshTokens.RemoveRange(tokens);
                await context.SaveChangesAsync();
            }
        }
    }
}