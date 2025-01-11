using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services
{
    public class CardService(ICardRepository repository, ILogger<CardService> logger) : ICardService
    {
        public async Task<CardDto> GetByIdAsync(int id)
        {
            try
            {
                logger.LogInformation("Retrieving card with ID: {Id}", id);
                var card = await repository.GetByIdAsync(id);
                
                if (card == null)
                {
                    logger.LogWarning("Card not found with ID: {Id}", id);
                    return null;
                }

                var dto = CardDto.FromEntity(card);
                logger.LogInformation("Successfully retrieved card with ID: {Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving card with ID: {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CardDto>> GetByRowIdAsync(int rowId)
        {
            try
            {
                logger.LogInformation("Retrieving cards for row ID: {RowId}", rowId);
                var cards = await repository.GetByRowIdAsync(rowId);
                var dtos = cards.Select(CardDto.FromEntity).ToList();
                logger.LogInformation("Retrieved {Count} cards for row ID: {RowId}", dtos.Count, rowId);
                return dtos;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving cards for row ID: {RowId}", rowId);
                throw;
            }
        }

        public async Task<CardDto> CreateAsync(int rowId, CardDto request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                logger.LogInformation("Creating new card in row ID: {RowId}", rowId);

                var order = await repository.GetMaxOrderInRowAsync(rowId) + 1;
                logger.LogDebug("Calculated order {Order} for new card in row {RowId}", order, rowId);

                var card = new Card
                {
                    Order = order,
                    Type = request.Type,
                    Configuration = request.Configuration != null 
                        ? JsonConvert.SerializeObject(request.Configuration)
                        : null,
                    RowId = rowId,
                    GridWidth = request.GridWidth,
                    BackgroundColor = request.BackgroundColor ?? 0xFF000000,
                    TextColor = request.TextColor ?? 0xFFFFFFFF,
                    HeaderBackgroundColor = request.HeaderBackgroundColor,
                    HeaderTextColor = request.HeaderTextColor
                };

                if (request.Titles?.Any() == true)
                {
                    card.CardTranslations = request.Titles.Select(t => new CardTranslation
                    {
                        LanguageCode = t.LanguageCode,
                        Title = t.Title
                    }).ToList();
                }

                var result = await repository.CreateAsync(card);
                var dto = CardDto.FromEntity(result);
                
                logger.LogInformation("Successfully created card with ID: {Id} in row: {RowId}", 
                    dto.Id, rowId);
                return dto;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation while creating card in row ID: {RowId}", rowId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating card in row ID: {RowId}", rowId);
                throw;
            }
        }

        public async Task<CardDto> UpdateAsync(int id, CardDto request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                logger.LogInformation("Updating card with ID: {Id}", id);

                var existingCard = await repository.GetByIdAsync(id);
                if (existingCard == null)
                {
                    logger.LogWarning("Card not found for update with ID: {Id}", id);
                    return null;
                }

                existingCard.Type = request.Type;
                existingCard.GridWidth = request.GridWidth;
                existingCard.Order = request.Order;
                existingCard.BackgroundColor = request.BackgroundColor;
                existingCard.TextColor = request.TextColor;
                existingCard.RowId = request.RowId;
                existingCard.HeaderBackgroundColor = request.HeaderBackgroundColor;
                existingCard.HeaderTextColor = request.HeaderTextColor;
                existingCard.Configuration = request.Configuration != null 
                    ? JsonConvert.SerializeObject(request.Configuration)
                    : null;

                existingCard.CardTranslations.Clear();
                if (request.Titles?.Any() == true)
                {
                    foreach (var translation in request.Titles)
                    {
                        existingCard.CardTranslations.Add(new CardTranslation
                        {
                            LanguageCode = translation.LanguageCode,
                            Title = translation.Title,
                            CardId = id
                        });
                    }
                }

                var result = await repository.UpdateAsync(existingCard);
                var dto = CardDto.FromEntity(result);

                logger.LogInformation("Successfully updated card with ID: {Id}", id);
                return dto;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation while updating card with ID: {Id}", id);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating card with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                logger.LogInformation("Deleting card with ID: {Id}", id);
                var result = await repository.DeleteAsync(id);
                
                if (result)
                {
                    logger.LogInformation("Successfully deleted card with ID: {Id}", id);
                }
                else
                {
                    logger.LogWarning("Card not found for deletion with ID: {Id}", id);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting card with ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ReorderAsync(int rowId, List<int> cardIds)
        {
            try
            {
                if (cardIds == null || !cardIds.Any())
                {
                    throw new ArgumentException("Card IDs list cannot be null or empty", nameof(cardIds));
                }

                logger.LogInformation("Reordering {Count} cards in row ID: {RowId}", cardIds.Count, rowId);

                var cards = await repository.GetByRowIdAsync(rowId);
                var cardDict = cards.ToDictionary(c => c.Id);

                var notFoundIds = cardIds.Where(id => !cardDict.ContainsKey(id)).ToList();
                if (notFoundIds.Any())
                {
                    logger.LogWarning("Some cards were not found during reordering. Card IDs: {@NotFoundIds}", 
                        notFoundIds);
                    return false;
                }

                for (int i = 0; i < cardIds.Count; i++)
                {
                    var card = cardDict[cardIds[i]];
                    card.Order = i + 1;
                    await repository.UpdateAsync(card);
                }

                logger.LogInformation("Successfully reordered cards in row ID: {RowId}", rowId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reordering cards in row ID: {RowId}", rowId);
                throw;
            }
        }
    }
} 