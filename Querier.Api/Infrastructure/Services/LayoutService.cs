using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services
{
    public class LayoutService(
        IPageRepository pageRepository,
        IRowRepository rowRepository,
        ICardService cardService,
        ILogger<LayoutService> logger)
        : ILayoutService
    {
        public async Task<LayoutDto> GetLayoutAsync(int pageId)
        {
            logger.LogInformation("Getting layout for page {PageId}", pageId);
            try
            {
                var page = await pageRepository.GetByIdAsync(pageId);
                if (page == null)
                {
                    logger.LogWarning("Page {PageId} not found, returning default layout", pageId);
                    return new LayoutDto
                    {
                        PageId = pageId,
                        Rows = new List<RowDto>()
                    };
                }

                var rows = await rowRepository.GetByPageIdAsync(pageId);
                var rowResponses = new List<RowDto>();

                foreach (var row in rows)
                {
                    try
                    {
                        var cards = await cardService.GetByRowIdAsync(row.Id);
                        rowResponses.Add(new RowDto
                        {
                            Id = row.Id,
                            Order = row.Order,
                            Height = row.Height,
                            Cards = cards
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error getting cards for row {RowId} in page {PageId}", row.Id, pageId);
                        // Continue with next row instead of failing completely
                        continue;
                    }
                }

                var layout = new LayoutDto
                {
                    PageId = page.Id,
                    Rows = rowResponses
                };

                logger.LogInformation("Successfully retrieved layout for page {PageId} with {RowCount} rows", pageId, rowResponses.Count);
                return layout;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving layout for page {PageId}", pageId);
                throw;
            }
        }

        public async Task<LayoutDto> UpdateLayoutAsync(int pageId, LayoutDto layout)
        {
            logger.LogInformation("Updating layout for page {PageId}", pageId);
            try
            {
                var page = await pageRepository.GetByIdAsync(pageId);
                if (page == null)
                {
                    logger.LogWarning("Page {PageId} not found during update", pageId);
                    return null;
                }

                await pageRepository.UpdateAsync(pageId, page);
                logger.LogInformation("Updated page {PageId} basic properties", pageId);

                // Mise à jour des rows et cards
                var existingRows = await rowRepository.GetByPageIdAsync(pageId);
                foreach (var row in existingRows)
                {
                    try
                    {
                        // On ne supprime plus les rows, on les met à jour
                        var updatedRow = layout.Rows.FirstOrDefault(r => r.Id == row.Id);
                        if (updatedRow == null)
                        {
                            // Si la row n'existe plus dans le nouveau layout, on la supprime
                            await rowRepository.DeleteAsync(row.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error updating row {RowId} during layout update for page {PageId}", row.Id, pageId);
                        throw;
                    }
                }

                foreach (var rowResponse in layout.Rows)
                {
                    try
                    {
                        Row savedRow;
                        if (rowResponse.Id > 0)
                        {
                            // Mise à jour de la row existante
                            var existingRow = await rowRepository.GetByIdAsync(rowResponse.Id);
                            if (existingRow != null)
                            {
                                existingRow.Order = rowResponse.Order;
                                existingRow.Height = rowResponse.Height;
                                savedRow = await rowRepository.UpdateAsync(rowResponse.Id, existingRow);
                            }
                            else
                            {
                                // La row n'existe plus, on en crée une nouvelle
                                savedRow = await rowRepository.CreateAsync(new Row
                                {
                                    PageId = pageId,
                                    Order = rowResponse.Order,
                                    Height = rowResponse.Height,
                                });
                            }
                        }
                        else
                        {
                            // Nouvelle row
                            savedRow = await rowRepository.CreateAsync(new Row
                            {
                                PageId = pageId,
                                Order = rowResponse.Order,
                                Height = rowResponse.Height,
                            });
                        }

                        foreach (var cardDto in rowResponse.Cards)
                        {
                            if (cardDto.Id > 0)
                            {
                                // Mise à jour de la carte existante
                                await cardService.UpdateAsync(cardDto.Id, savedRow.Id, cardDto);
                            }
                            else
                            {
                                // Nouvelle carte
                                await cardService.CreateAsync(savedRow.Id, cardDto);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error creating/updating row and cards during layout update for page {PageId}", pageId);
                        throw;
                    }
                }

                logger.LogInformation("Successfully updated layout for page {PageId} with {RowCount} rows", pageId, layout.Rows.Count);
                return await GetLayoutAsync(pageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating layout for page {PageId}", pageId);
                throw;
            }
        }

        public async Task<bool> DeleteLayoutAsync(int pageId)
        {
            logger.LogInformation("Deleting layout for page {PageId}", pageId);
            try
            {
                var result = await pageRepository.DeleteAsync(pageId);
                if (result)
                {
                    logger.LogInformation("Successfully deleted layout for page {PageId}", pageId);
                }
                else
                {
                    logger.LogWarning("Failed to delete layout for page {PageId} - page not found", pageId);
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting layout for page {PageId}", pageId);
                throw;
            }
        }
    }
} 