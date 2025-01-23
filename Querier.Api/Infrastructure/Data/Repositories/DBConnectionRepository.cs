using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class DbConnectionRepository : IDbConnectionRepository
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<DbConnectionRepository> _logger;

        public DbConnectionRepository(
            IDbContextFactory<ApiDbContext> contextFactory,
            ILogger<DbConnectionRepository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<int> AddDbConnectionAsync(DBConnection dbConnection)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dbConnection);
                _logger.LogInformation("Adding new database connection: {Name}", dbConnection.Name);

                using var context = await _contextFactory.CreateDbContextAsync();
                await context.DBConnections.AddAsync(dbConnection);
                var result = await context.SaveChangesAsync();

                _logger.LogInformation("Successfully added database connection: {Name} with ID: {Id}", 
                    dbConnection.Name, dbConnection.Id);
                return result;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while adding connection: {Name}", dbConnection?.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding database connection: {Name}", dbConnection?.Name);
                throw;
            }
        }

        public async Task DeleteDbConnectionAsync(int dbConnectionId)
        {
            try
            {
                _logger.LogInformation("Deleting database connection with ID: {Id}", dbConnectionId);

                using var context = await _contextFactory.CreateDbContextAsync();
                var toDelete = await context.DBConnections.FindAsync(dbConnectionId);
                
                if (toDelete == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", dbConnectionId);
                    throw new KeyNotFoundException($"Connection with ID {dbConnectionId} not found");
                }

                context.DBConnections.Remove(toDelete);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted database connection: {Name} with ID: {Id}", 
                    toDelete.Name, dbConnectionId);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting connection with ID: {Id}", dbConnectionId);
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting database connection with ID: {Id}", dbConnectionId);
                throw;
            }
        }

        public async Task<List<DBConnection>> GetAllDbConnectionsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all database connections");

                using var context = await _contextFactory.CreateDbContextAsync();
                var connections = await context.DBConnections
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.Endpoints)
                        .ThenInclude(e => e.Parameters)
                    .Include(c => c.Endpoints)
                        .ThenInclude(e => e.Responses)
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} database connections", connections.Count);
                return connections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all database connections");
                throw;
            }
        }

        public async Task<DBConnection> FindByIdAsync(int dbConnectionId)
        {
            try
            {
                _logger.LogInformation("Finding database connection with ID: {Id}", dbConnectionId);

                using var context = await _contextFactory.CreateDbContextAsync();
                var connection = await context.DBConnections
                    .AsNoTracking()
                    .Include(c => c.Endpoints)
                        .ThenInclude(e => e.Parameters)
                    .Include(c => c.Endpoints)
                        .ThenInclude(e => e.Responses)
                    .FirstOrDefaultAsync(c => c.Id == dbConnectionId);

                if (connection == null)
                {
                    _logger.LogWarning("Database connection not found with ID: {Id}", dbConnectionId);
                    throw new KeyNotFoundException($"Connection with ID {dbConnectionId} not found");
                }

                _logger.LogInformation("Successfully found database connection: {Name} with ID: {Id}", 
                    connection.Name, dbConnectionId);
                return connection;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding database connection with ID: {Id}", dbConnectionId);
                throw;
            }
        }
    }
}