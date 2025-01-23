using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;
using System.Linq;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class PageRepository(ApiDbContext context, ILogger<PageRepository> logger) : IPageRepository
    {
        public async Task<Page> GetByIdAsync(int id)
        {
            logger.LogDebug("Getting page by ID {PageId}", id);
            try
            {
                var page = await context.Pages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (page == null)
                {
                    logger.LogWarning("Page {PageId} not found", id);
                }
                else
                {
                    logger.LogDebug("Successfully retrieved page {PageId}", id);
                }
                return page;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving page {PageId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Page>> GetAllAsync()
        {
            logger.LogDebug("Getting all pages");
            try
            {
                var pages = await context.Pages
                    .AsNoTracking()
                    .ToListAsync();
                logger.LogDebug("Retrieved {Count} pages", pages.Count);
                return pages;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all pages");
                throw;
            }
        }

        public async Task<Page> CreateAsync(Page page)
        {
            if (page == null)
            {
                logger.LogError("Attempted to create a null page");
                throw new ArgumentNullException(nameof(page));
            }

            logger.LogDebug("Creating new page");
            try
            {
                context.Pages.Add(page);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully created page {PageId}", page.Id);
                return page;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while creating page");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating page");
                throw;
            }
        }

        public async Task<Page> UpdateAsync(int id, Page page)
        {
            if (page == null)
            {
                logger.LogError("Attempted to update page {PageId} with null data", id);
                throw new ArgumentNullException(nameof(page));
            }

            logger.LogDebug("Updating page {PageId}", id);
            try
            {
                var existingPage = await context.Pages
                    .Include(p => p.PageTranslations)
                    .FirstOrDefaultAsync(p => p.Id == id);
                
                if (existingPage == null)
                {
                    logger.LogWarning("Page {PageId} not found for update", id);
                    return null;
                }

                await context.SaveChangesAsync();
                logger.LogInformation("Successfully updated page {PageId}", id);
                return existingPage;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogError(ex, "Concurrency error while updating page {PageId}", id);
                throw;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while updating page {PageId}", id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating page {PageId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            logger.LogDebug("Deleting page {PageId}", id);
            try
            {
                var page = await GetByIdAsync(id);
                if (page == null)
                {
                    logger.LogWarning("Cannot delete page {PageId} - not found", id);
                    return false;
                }

                context.Pages.Remove(page);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully deleted page {PageId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error while deleting page {PageId}", id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting page {PageId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Page>> GetAllByMenuIdAsync(int menuId)
        {
            logger.LogDebug("Getting pages for menu {MenuId}", menuId);
            try
            {
                var pages = await context.Pages
                    .AsNoTracking()
                    .Where(p => p.MenuId == menuId)
                    .ToListAsync();
                logger.LogDebug("Retrieved {Count} pages for menu {MenuId}", pages.Count, menuId);
                return pages;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving pages for menu {MenuId}", menuId);
                throw;
            }
        }
    }
}