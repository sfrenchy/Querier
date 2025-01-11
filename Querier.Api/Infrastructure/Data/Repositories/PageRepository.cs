using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class PageRepository(ApiDbContext context, ILogger<PageRepository> logger) : IPageRepository
    {
        public async Task<Page> GetByIdAsync(int id)
        {
            logger.LogDebug("Getting page by ID {PageId}", id);
            try
            {
                var page = await context.Pages.FindAsync(id);
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
                var pages = await context.Pages.ToListAsync();
                logger.LogDebug("Retrieved {PageCount} pages", pages.Count);
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

                // Mise à jour des propriétés simples
                existingPage.Icon = page.Icon;
                existingPage.Order = page.Order;
                existingPage.IsVisible = page.IsVisible;
                existingPage.Roles = page.Roles;
                existingPage.Route = page.Route;
                existingPage.MenuId = page.MenuId;

                // Mise à jour des traductions
                existingPage.PageTranslations.Clear();
                foreach (var translation in page.PageTranslations)
                {
                    existingPage.PageTranslations.Add(new PageTranslation
                    {
                        LanguageCode = translation.LanguageCode,
                        Name = translation.Name
                    });
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
    }
}