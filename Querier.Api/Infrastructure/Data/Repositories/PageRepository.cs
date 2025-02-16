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
    public class PageRepository : IPageRepository
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<PageRepository> _logger;

        public PageRepository(IDbContextFactory<ApiDbContext> contextFactory, ILogger<PageRepository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Page> GetByIdAsync(int id, bool includeRelations = true)
        {
            _logger.LogDebug("Getting page by ID {PageId}", id);
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                IQueryable<Page> query = context.Pages;
                
                if (includeRelations)
                {
                    query = query
                        .Include(p => p.PageTranslations)
                        .Include(p => p.Rows)
                            .ThenInclude(r => r.Cards)
                                .ThenInclude(c => c.CardTranslations);
                }

                var page = await query.FirstOrDefaultAsync(p => p.Id == id);
                if (page == null)
                {
                    _logger.LogWarning("Page {PageId} not found", id);
                }
                else
                {
                    _logger.LogDebug("Successfully retrieved page {PageId}", id);
                }
                return page;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving page {PageId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Page>> GetAllAsync()
        {
            _logger.LogDebug("Getting all pages");
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var pages = await context.Pages
                    .AsNoTracking()
                    .ToListAsync();
                _logger.LogDebug("Retrieved {Count} pages", pages.Count);
                return pages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all pages");
                throw;
            }
        }

        public async Task<Page> CreateAsync(Page page)
        {
            if (page == null)
            {
                _logger.LogError("Attempted to create a null page");
                throw new ArgumentNullException(nameof(page));
            }

            _logger.LogDebug("Creating new page");
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                context.Pages.Add(page);
                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully created page {PageId}", page.Id);
                return page;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating page");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page");
                throw;
            }
        }

        public async Task<Page> UpdateAsync(int id, Page page)
        {
            if (page == null)
            {
                _logger.LogError("Attempted to update page {PageId} with null data", id);
                throw new ArgumentNullException(nameof(page));
            }

            _logger.LogDebug("Updating page {PageId}", id);
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var existingPage = await context.Pages
                    .AsSplitQuery()
                    .Include(p => p.PageTranslations)
                    .FirstOrDefaultAsync(p => p.Id == id);
                
                if (existingPage == null)
                {
                    _logger.LogWarning("Page {PageId} not found for update", id);
                    return null;
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated page {PageId}", id);
                return existingPage;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating page {PageId}", id);
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating page {PageId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page {PageId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogDebug("Deleting page {PageId}", id);
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var page = await context.Pages.FindAsync(id);
                if (page == null)
                {
                    _logger.LogWarning("Cannot delete page {PageId} - not found", id);
                    return false;
                }

                context.Pages.Remove(page);
                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted page {PageId}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting page {PageId}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page {PageId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Page>> GetAllByMenuIdAsync(int menuId)
        {
            _logger.LogDebug("Getting pages for menu {MenuId}", menuId);
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var pages = await context.Pages
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(p => p.PageTranslations)
                    .Include(p => p.Rows)
                    .Where(p => p.MenuId == menuId)
                    .OrderBy(p => p.Order)
                    .ToListAsync();
                _logger.LogDebug("Retrieved {Count} pages for menu {MenuId}", pages.Count, menuId);
                return pages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pages for menu {MenuId}", menuId);
                throw;
            }
        }
    }
}