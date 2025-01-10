using System.Collections.Generic;
using System.Data.Entity;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class DbConnectionRepository(IDbContextFactory<ApiDbContext> contextFactory) : IDbConnectionRepository
    {
        public async Task<int> AddDbConnectionAsync(DBConnection dbConnection)
        {
            using (var context = await contextFactory.CreateDbContextAsync())
            {
                await context.DBConnections.AddAsync(dbConnection);
                return await context.SaveChangesAsync();
            }
        }

        public async Task DeleteDbConnectionAsync(int dbConnectionId)
        {
            using (var context = await contextFactory.CreateDbContextAsync())
            {
                var toDelete = context.DBConnections.Find(dbConnectionId);
                if (toDelete == null)
                    throw new KeyNotFoundException($"Connection with ID {dbConnectionId} not found");
                context.DBConnections.Remove(toDelete);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<DBConnection>> GetAllDbConnectionsAsync()
        {
            using (var context = await contextFactory.CreateDbContextAsync())
            {
                return await context.DBConnections.ToListAsync();
            }
        }

        public async Task<DBConnection> FindByIdAsync(int dbConnectionId)
        {
            using (var context = await contextFactory.CreateDbContextAsync())
            {
                var connection = await context.DBConnections.FindAsync(dbConnectionId);
            
                if (connection == null)
                    throw new KeyNotFoundException($"Connection with ID {dbConnectionId} not found");

                return connection;
            }
        }
    }
}