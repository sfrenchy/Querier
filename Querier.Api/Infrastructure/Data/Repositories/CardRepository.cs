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
    public class CardRepository(ApiDbContext context, ILogger<CardRepository> logger) : ICardRepository
    {
        public async Task<Card> GetByIdAsync(int id)
        {
            try
            {
                logger.LogDebug("Retrieving card with ID: {Id}", id);
                var card = await context.Cards
                    .Include(c => c.CardTranslations)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (card == null)
                {
                    logger.LogWarning("Card not found with ID: {Id}", id);
                    return null;
                }

                logger.LogDebug("Successfully retrieved card with ID: {Id}", id);
                return card;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving card with ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Card>> GetByRowIdAsync(int rowId)
        {
            try
            {
                logger.LogDebug("Retrieving cards for row ID: {RowId}", rowId);
                var cards = await context.Cards
                    .Include(c => c.CardTranslations)
                    .Where(c => c.RowId == rowId)
                    .OrderBy(c => c.Order)
                    .ToListAsync();

                logger.LogDebug("Retrieved {Count} cards for row ID: {RowId}", cards.Count, rowId);
                return cards;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving cards for row ID: {RowId}", rowId);
                throw;
            }
        }

        public async Task<Card> CreateAsync(Card card)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(card);

                logger.LogInformation("Creating new card for row ID: {RowId}", card.RowId);
                
                // Validate row exists
                var rowExists = await context.Rows.AnyAsync(r => r.Id == card.RowId);
                if (!rowExists)
                {
                    logger.LogWarning("Cannot create card: Row not found with ID: {RowId}", card.RowId);
                    throw new InvalidOperationException($"Row with ID {card.RowId} does not exist");
                }

                await context.Cards.AddAsync(card);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully created card with ID: {Id} in row: {RowId}", 
                    card.Id, card.RowId);
                return card;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error occurred while creating card for row ID: {RowId}", 
                    card?.RowId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating card for row ID: {RowId}", card?.RowId);
                throw;
            }
        }

        public async Task<Card> UpdateAsync(Card card)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(card);

                logger.LogInformation("Updating card with ID: {Id}", card.Id);

                var exists = await context.Cards.AnyAsync(c => c.Id == card.Id);
                if (!exists)
                {
                    logger.LogWarning("Cannot update card: Card not found with ID: {Id}", card.Id);
                    throw new InvalidOperationException($"Card with ID {card.Id} does not exist");
                }

                context.Cards.Update(card);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully updated card with ID: {Id}", card.Id);
                return card;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error occurred while updating card with ID: {Id}", card?.Id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating card with ID: {Id}", card?.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                logger.LogInformation("Deleting card with ID: {Id}", id);

                var card = await GetByIdAsync(id);
                if (card == null)
                {
                    logger.LogWarning("Cannot delete card: Card not found with ID: {Id}", id);
                    return false;
                }

                context.Cards.Remove(card);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully deleted card with ID: {Id}", id);
                return true;
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error occurred while deleting card with ID: {Id}", id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting card with ID: {Id}", id);
                throw;
            }
        }

        public async Task<int> GetMaxOrderInRowAsync(int rowId)
        {
            try
            {
                logger.LogDebug("Getting maximum order value for row ID: {RowId}", rowId);
                var maxOrder = await context.Cards
                    .Where(c => c.RowId == rowId)
                    .MaxAsync(c => (int?)c.Order) ?? 0;

                logger.LogDebug("Maximum order value for row ID {RowId} is: {MaxOrder}", rowId, maxOrder);
                return maxOrder;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting maximum order value for row ID: {RowId}", rowId);
                throw;
            }
        }
    }
} 