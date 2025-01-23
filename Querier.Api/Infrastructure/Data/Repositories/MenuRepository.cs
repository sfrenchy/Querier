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
    public class MenuRepository : IMenuRepository
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<MenuRepository> _logger;

        public MenuRepository(IDbContextFactory<ApiDbContext> contextFactory, ILogger<MenuRepository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Menu> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving menu with ID: {Id}", id);
                using var context = await _contextFactory.CreateDbContextAsync();
                var menu = await context.Menus
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(m => m.Translations)
                    .Include(m => m.Pages)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (menu == null)
                {
                    _logger.LogWarning("Menu not found with ID: {Id}", id);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved menu with ID: {Id}", id);
                return menu;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving menu with ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<Menu>> GetAllAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all menus");
                using var context = await _contextFactory.CreateDbContextAsync();
                var menus = await context.Menus
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(m => m.Translations)
                    .Include(m => m.Pages)
                    .OrderBy(m => m.Order)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} menus", menus.Count);
                return menus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all menus");
                throw;
            }
        }

        public async Task<Menu> CreateAsync(Menu menu)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(menu);

                _logger.LogInformation("Creating new menu");
                using var context = await _contextFactory.CreateDbContextAsync();
                await context.Menus.AddAsync(menu);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully created menu with ID: {Id}", menu.Id);
                return menu;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while creating menu");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating menu");
                throw;
            }
        }

        public async Task<Menu> UpdateAsync(Menu menu)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(menu);

                _logger.LogInformation("Updating menu with ID: {Id}", menu.Id);
                using var context = await _contextFactory.CreateDbContextAsync();

                var exists = await context.Menus.AnyAsync(m => m.Id == menu.Id);
                if (!exists)
                {
                    _logger.LogWarning("Cannot update menu: Menu not found with ID: {Id}", menu.Id);
                    throw new InvalidOperationException($"Menu with ID {menu.Id} does not exist");
                }

                context.Menus.Update(menu);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated menu with ID: {Id}", menu.Id);
                return menu;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating menu with ID: {Id}", menu?.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating menu with ID: {Id}", menu?.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting menu with ID: {Id}", id);
                using var context = await _contextFactory.CreateDbContextAsync();

                var menu = await context.Menus.FindAsync(id);
                if (menu == null)
                {
                    _logger.LogWarning("Cannot delete menu: Menu not found with ID: {Id}", id);
                    return false;
                }

                context.Menus.Remove(menu);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted menu with ID: {Id}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while deleting menu with ID: {Id}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting menu with ID: {Id}", id);
                throw;
            }
        }

        public async Task<int> GetMaxOrderAsync()
        {
            try
            {
                _logger.LogDebug("Getting maximum order value for menus");
                using var context = await _contextFactory.CreateDbContextAsync();
                var maxOrder = await context.Menus
                    .AsNoTracking()
                    .MaxAsync(m => (int?)m.Order) ?? 0;

                _logger.LogDebug("Maximum order value for menus is: {MaxOrder}", maxOrder);
                return maxOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting maximum order value for menus");
                throw;
            }
        }
    }
} 