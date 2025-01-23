using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class RowRepository : IRowRepository
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<RowRepository> _logger;

        public RowRepository(IDbContextFactory<ApiDbContext> contextFactory, ILogger<RowRepository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Row> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving row with ID: {Id}", id);
                using var context = await _contextFactory.CreateDbContextAsync();
                var row = await context.Rows
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(r => r.Cards)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (row == null)
                {
                    _logger.LogWarning("Row not found with ID: {Id}", id);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved row with ID: {Id}", id);
                return row;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving row with ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Row>> GetByPageIdAsync(int pageId)
        {
            try
            {
                _logger.LogDebug("Retrieving rows for page ID: {PageId}", pageId);
                using var context = await _contextFactory.CreateDbContextAsync();
                var rows = await context.Rows
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(r => r.Cards)
                    .Where(r => r.PageId == pageId)
                    .OrderBy(r => r.Order)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} rows for page ID: {PageId}", rows.Count, pageId);
                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rows for page ID: {PageId}", pageId);
                throw;
            }
        }

        public async Task<Row> CreateAsync(Row row)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(row);

                _logger.LogInformation("Creating new row for page ID: {PageId}", row.PageId);
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Validate page exists
                var pageExists = await context.Pages.AnyAsync(p => p.Id == row.PageId);
                if (!pageExists)
                {
                    _logger.LogWarning("Cannot create row: Page not found with ID: {PageId}", row.PageId);
                    throw new InvalidOperationException($"Page with ID {row.PageId} does not exist");
                }

                await context.Rows.AddAsync(row);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully created row with ID: {Id} in page: {PageId}", 
                    row.Id, row.PageId);
                return row;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while creating row for page ID: {PageId}", 
                    row?.PageId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating row for page ID: {PageId}", row?.PageId);
                throw;
            }
        }

        public async Task<Row> UpdateAsync(Row row)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(row);

                _logger.LogInformation("Updating row with ID: {Id}", row.Id);
                using var context = await _contextFactory.CreateDbContextAsync();

                var exists = await context.Rows.AnyAsync(r => r.Id == row.Id);
                if (!exists)
                {
                    _logger.LogWarning("Cannot update row: Row not found with ID: {Id}", row.Id);
                    throw new InvalidOperationException($"Row with ID {row.Id} does not exist");
                }

                context.Rows.Update(row);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated row with ID: {Id}", row.Id);
                return row;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating row with ID: {Id}", row?.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating row with ID: {Id}", row?.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting row with ID: {Id}", id);
                using var context = await _contextFactory.CreateDbContextAsync();

                var row = await context.Rows.FindAsync(id);
                if (row == null)
                {
                    _logger.LogWarning("Cannot delete row: Row not found with ID: {Id}", id);
                    return false;
                }

                context.Rows.Remove(row);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted row with ID: {Id}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while deleting row with ID: {Id}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting row with ID: {Id}", id);
                throw;
            }
        }

        public async Task<int> GetMaxOrderInPageAsync(int pageId)
        {
            try
            {
                _logger.LogDebug("Getting maximum order value for page ID: {PageId}", pageId);
                using var context = await _contextFactory.CreateDbContextAsync();
                var maxOrder = await context.Rows
                    .AsNoTracking()
                    .Where(r => r.PageId == pageId)
                    .MaxAsync(r => (int?)r.Order) ?? 0;

                _logger.LogDebug("Maximum order value for page ID {PageId} is: {MaxOrder}", pageId, maxOrder);
                return maxOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting maximum order value for page ID: {PageId}", pageId);
                throw;
            }
        }
    }
} 