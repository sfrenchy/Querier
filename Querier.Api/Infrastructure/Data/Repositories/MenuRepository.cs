using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class MenuRepository(ApiDbContext context, ILogger<MenuRepository> logger) : IMenuRepository
    {
        public async Task<List<Domain.Entities.Menu.Menu>> GetAllAsync()
        {
            logger.LogDebug("Getting all menu categories");
            try
            {
                var categories = await context.Menus
                    .AsNoTracking()
                    .Include(x => x.Translations)
                    .ToListAsync();

                logger.LogDebug("Retrieved {Count} menu categories", categories.Count);
                return categories;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all menu categories");
                throw;
            }
        }

        public async Task<Domain.Entities.Menu.Menu> GetByIdAsync(int id)
        {
            logger.LogDebug("Getting menu category {CategoryId}", id);
            try
            {
                var category = await context.Menus
                    .AsNoTracking()
                    .Include(x => x.Translations)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (category == null)
                {
                    logger.LogWarning("Menu category {CategoryId} not found", id);
                }
                else
                {
                    logger.LogDebug("Successfully retrieved menu category {CategoryId}", id);
                }

                return category;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving menu category {CategoryId}", id);
                throw;
            }
        }

        public async Task<Domain.Entities.Menu.Menu> CreateAsync(Domain.Entities.Menu.Menu category)
        {
            if (category == null)
            {
                logger.LogError("Attempted to create a null menu category");
                throw new ArgumentNullException(nameof(category));
            }

            logger.LogDebug("Creating new menu category");
            try
            {
                await context.Menus.AddAsync(category);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully created menu category {CategoryId}", category.Id);
                return category;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while creating menu category");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating menu category");
                throw;
            }
        }

        public async Task<Domain.Entities.Menu.Menu> UpdateAsync(Domain.Entities.Menu.Menu category)
        {
            if (category == null)
            {
                logger.LogError("Attempted to update a null menu category");
                throw new ArgumentNullException(nameof(category));
            }

            logger.LogDebug("Updating menu category {CategoryId}", category.Id);
            try
            {
                context.Menus.Update(category);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully updated menu category {CategoryId}", category.Id);
                return category;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Concurrency error while updating menu category {CategoryId}", category.Id);
                throw;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while updating menu category {CategoryId}", category.Id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating menu category {CategoryId}", category.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            logger.LogDebug("Deleting menu category {CategoryId}", id);
            try
            {
                var category = await context.Menus.FindAsync(id);
                if (category == null)
                {
                    logger.LogWarning("Cannot delete menu category {CategoryId} - not found", id);
                    return false;
                }

                context.Menus.Remove(category);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully deleted menu category {CategoryId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while deleting menu category {CategoryId}", id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting menu category {CategoryId}", id);
                throw;
            }
        }
    }
} 