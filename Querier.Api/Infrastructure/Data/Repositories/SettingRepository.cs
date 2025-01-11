using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Common.Metadata;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class SettingRepository : ISettingRepository
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<SettingRepository> _logger;

        public SettingRepository(
            IDbContextFactory<ApiDbContext> contextFactory,
            ILogger<SettingRepository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<Setting>> ListAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving all settings");
                using var context = await _contextFactory.CreateDbContextAsync();
                var settings = await context.Settings.ToListAsync();
                _logger.LogDebug("Successfully retrieved {Count} settings", settings.Count);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve settings");
                throw;
            }
        }

        public async Task<Setting> GetByIdAsync(int settingId)
        {
            try
            {
                _logger.LogDebug("Retrieving setting with ID: {Id}", settingId);

                using var context = await _contextFactory.CreateDbContextAsync();
                var setting = await context.Settings.SingleOrDefaultAsync(s => s.Id == settingId);

                if (setting == null)
                {
                    _logger.LogWarning("Setting not found with ID: {Id}", settingId);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved setting: {Name} (ID: {Id})", setting.Name, settingId);
                return setting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve setting with ID: {Id}", settingId);
                throw;
            }
        }
        
        public async Task<Setting> UpdateAsync(Setting setting)
        {
            try
            {
                if (setting == null)
                {
                    _logger.LogError("UpdateAsync called with null setting");
                    throw new ArgumentNullException(nameof(setting));
                }

                _logger.LogDebug("Updating setting: {Name} (ID: {Id})", setting.Name, setting.Id);

                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Vérifier si le paramètre existe
                var exists = await context.Settings.AnyAsync(s => s.Id == setting.Id);
                if (!exists)
                {
                    _logger.LogWarning("Setting not found for update: {Name} (ID: {Id})", setting.Name, setting.Id);
                    throw new KeyNotFoundException($"Setting with ID {setting.Id} not found");
                }

                context.Settings.Update(setting);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated setting: {Name} (ID: {Id})", setting.Name, setting.Id);
                return setting;
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not KeyNotFoundException)
            {
                _logger.LogError(ex, "Failed to update setting: {Name} (ID: {Id})", setting?.Name, setting?.Id);
                throw;
            }
        }

        public async Task AddAsync(Setting setting)
        {
            try
            {
                if (setting == null)
                {
                    _logger.LogError("AddAsync called with null setting");
                    throw new ArgumentNullException(nameof(setting));
                }

                _logger.LogDebug("Adding new setting: {Name}", setting.Name);

                using var context = await _contextFactory.CreateDbContextAsync();

                // Vérifier si un paramètre avec le même nom existe déjà
                var exists = await context.Settings.AnyAsync(s => s.Name == setting.Name);
                if (exists)
                {
                    var error = $"Setting with name '{setting.Name}' already exists";
                    _logger.LogWarning(error);
                    throw new InvalidOperationException(error);
                }

                await context.Settings.AddAsync(setting);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully added new setting: {Name} (ID: {Id})", setting.Name, setting.Id);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to add setting: {Name}", setting?.Name);
                throw;
            }
        }
    }
}