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
    public class RowRepository(ApiDbContext context, ILogger<RowRepository> logger) : IRowRepository
    {
        public async Task<Row> GetByIdAsync(int id)
        {
            logger.LogDebug("Getting row by ID {RowId}", id);
            try
            {
                var row = await context.Rows
                    .Include(r => r.Cards)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (row == null)
                {
                    logger.LogWarning("Row {RowId} not found", id);
                }
                else
                {
                    logger.LogDebug("Successfully retrieved row {RowId}", id);
                }

                return row;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving row {RowId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Row>> GetByPageIdAsync(int pageId)
        {
            logger.LogDebug("Getting rows for page {PageId}", pageId);
            try
            {
                var rows = await context.Rows
                    .Include(r => r.Cards)
                    .Where(r => r.PageId == pageId)
                    .OrderBy(r => r.Order)
                    .ToListAsync();

                logger.LogDebug("Retrieved {RowCount} rows for page {PageId}", rows.Count, pageId);
                return rows;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving rows for page {PageId}", pageId);
                throw;
            }
        }

        public async Task<Row> CreateAsync(Row row)
        {
            if (row == null)
            {
                logger.LogError("Attempted to create a null row");
                throw new ArgumentNullException(nameof(row));
            }

            logger.LogDebug("Creating new row for page {PageId}", row.PageId);
            try
            {
                await context.Rows.AddAsync(row);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully created row {RowId} for page {PageId}", row.Id, row.PageId);
                return row;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while creating row for page {PageId}", row.PageId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating row for page {PageId}", row.PageId);
                throw;
            }
        }

        public async Task<Row> UpdateAsync(Row row)
        {
            if (row == null)
            {
                logger.LogError("Attempted to update a null row");
                throw new ArgumentNullException(nameof(row));
            }

            logger.LogDebug("Updating row {RowId}", row.Id);
            try
            {
                context.Rows.Update(row);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully updated row {RowId}", row.Id);
                return row;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Concurrency error while updating row {RowId}", row.Id);
                throw;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while updating row {RowId}", row.Id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating row {RowId}", row.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            logger.LogDebug("Deleting row {RowId}", id);
            try
            {
                var row = await GetByIdAsync(id);
                if (row == null)
                {
                    logger.LogWarning("Cannot delete row {RowId} - not found", id);
                    return false;
                }

                context.Rows.Remove(row);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully deleted row {RowId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while deleting row {RowId}", id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting row {RowId}", id);
                throw;
            }
        }

        public async Task<int> GetMaxOrderInPageAsync(int pageId)
        {
            logger.LogDebug("Getting max order for page {PageId}", pageId);
            try
            {
                var maxOrder = await context.Rows
                    .Where(r => r.PageId == pageId)
                    .MaxAsync(r => (int?)r.Order) ?? 0;

                logger.LogDebug("Max order for page {PageId} is {MaxOrder}", pageId, maxOrder);
                return maxOrder;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting max order for page {PageId}", pageId);
                throw;
            }
        }
    }
} 