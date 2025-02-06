using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class CardRepository : ICardRepository
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<CardRepository> _logger;

        public CardRepository(IDbContextFactory<ApiDbContext> contextFactory, ILogger<CardRepository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Card> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving card with ID: {Id}", id);
                using var context = await _contextFactory.CreateDbContextAsync();
                var card = await context.Cards
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.CardTranslations)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (card == null)
                {
                    _logger.LogWarning("Card not found with ID: {Id}", id);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved card with ID: {Id}", id);
                return card;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving card with ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Card>> GetByRowIdAsync(int rowId)
        {
            try
            {
                _logger.LogDebug("Retrieving cards for row ID: {RowId}", rowId);
                using var context = await _contextFactory.CreateDbContextAsync();
                var cards = await context.Cards
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.CardTranslations)
                    .Where(c => c.RowId == rowId)
                    .OrderBy(c => c.Order)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} cards for row ID: {RowId}", cards.Count, rowId);
                return cards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cards for row ID: {RowId}", rowId);
                throw;
            }
        }

        public async Task<Card> CreateAsync(Card card)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(card);

                _logger.LogInformation("Creating new card for row ID: {RowId}", card.RowId);
                using var context = await _contextFactory.CreateDbContextAsync();
                
                // Validate row exists
                var rowExists = await context.Rows.AnyAsync(r => r.Id == card.RowId);
                if (!rowExists)
                {
                    _logger.LogWarning("Cannot create card: Row not found with ID: {RowId}", card.RowId);
                    throw new InvalidOperationException($"Row with ID {card.RowId} does not exist");
                }

                await context.Cards.AddAsync(card);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully created card with ID: {Id} in row: {RowId}", 
                    card.Id, card.RowId);
                return card;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while creating card for row ID: {RowId}", 
                    card?.RowId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating card for row ID: {RowId}", card?.RowId);
                throw;
            }
        }

        public async Task<Card> UpdateAsync(Card card)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(card);

                _logger.LogInformation("Updating card with ID: {Id}", card.Id);
                using var context = await _contextFactory.CreateDbContextAsync();

                // Récupérer la carte existante avec ses traductions
                var existingCard = await context.Cards
                    .Include(c => c.CardTranslations)
                    .FirstOrDefaultAsync(c => c.Id == card.Id);

                if (existingCard == null)
                {
                    _logger.LogWarning("Cannot update card: Card not found with ID: {Id}", card.Id);
                    throw new InvalidOperationException($"Card with ID {card.Id} does not exist");
                }

                // Mettre à jour les propriétés de base
                context.Entry(existingCard).CurrentValues.SetValues(card);

                // Gérer les traductions
                // Supprimer toutes les anciennes traductions
                context.CardTranslations.RemoveRange(existingCard.CardTranslations);

                // Ajouter les nouvelles traductions
                if (card.CardTranslations != null)
                {
                    foreach (var translation in card.CardTranslations)
                    {
                        existingCard.CardTranslations.Add(translation);
                    }
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated card with ID: {Id}", card.Id);
                return existingCard;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating card with ID: {Id}", card?.Id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating card with ID: {Id}", card?.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting card with ID: {Id}", id);
                using var context = await _contextFactory.CreateDbContextAsync();

                var card = await context.Cards.FindAsync(id);
                if (card == null)
                {
                    _logger.LogWarning("Cannot delete card: Card not found with ID: {Id}", id);
                    return false;
                }

                context.Cards.Remove(card);
                await context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted card with ID: {Id}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while deleting card with ID: {Id}", id);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting card with ID: {Id}", id);
                throw;
            }
        }

        public async Task<int> GetMaxOrderInRowAsync(int rowId)
        {
            try
            {
                _logger.LogDebug("Getting maximum order value for row ID: {RowId}", rowId);
                using var context = await _contextFactory.CreateDbContextAsync();
                var maxOrder = await context.Cards
                    .AsNoTracking()
                    .Where(c => c.RowId == rowId)
                    .MaxAsync(c => (int?)c.Order) ?? 0;

                _logger.LogDebug("Maximum order value for row ID {RowId} is: {MaxOrder}", rowId, maxOrder);
                return maxOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting maximum order value for row ID: {RowId}", rowId);
                throw;
            }
        }

        public async Task<DataPagedResult<Card>> GetByRowIdPagedAsync(int rowId, DataRequestParametersDto parameters)
        {
            try
            {
                _logger.LogDebug("Retrieving paged cards for row ID: {RowId}, Page: {PageNumber}, Size: {PageSize}", 
                    rowId, parameters.PageNumber, parameters.PageSize);
                
                using var context = await _contextFactory.CreateDbContextAsync();
                var query = context.Cards
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(c => c.CardTranslations)
                    .Where(c => c.RowId == rowId);

                // Apply global search if specified
                if (!string.IsNullOrWhiteSpace(parameters.GlobalSearch))
                {
                    var search = parameters.GlobalSearch.ToLower();
                    query = query.Where(c => c.CardTranslations.Any(ct => 
                        ct.Title.ToLower().Contains(search)));
                }

                // Apply column searches if any
                if (parameters.ColumnSearches?.Any() == true)
                {
                    foreach (var columnSearch in parameters.ColumnSearches)
                    {
                        // Add specific column search logic here if needed
                        // Example: query = query.Where(...);
                    }
                }

                // Apply ordering
                if (parameters.OrderBy?.Any() == true)
                {
                    // Apply custom ordering if specified
                    foreach (var orderBy in parameters.OrderBy)
                    {
                        // Add specific ordering logic here if needed
                        // Example: query = query.OrderBy(...)
                    }
                }
                else
                {
                    // Default ordering by Order field
                    query = query.OrderBy(c => c.Order);
                }

                // Get total count
                var total = await query.CountAsync();

                // Apply pagination
                var items = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync();

                _logger.LogDebug("Retrieved {Count} cards from total {Total} for row ID: {RowId}", 
                    items.Count, total, rowId);

                return new DataPagedResult<Card>(items, total, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged cards for row ID: {RowId}", rowId);
                throw;
            }
        }
    }
} 