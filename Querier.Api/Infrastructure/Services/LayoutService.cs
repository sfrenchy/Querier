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
                        Icon = "settings",
                        Names = new Dictionary<string, string>
                        {
                            { "en", "Page Not Found" },
                            { "fr", "Page Non Trouvée" }
                        },
                        IsVisible = true,
                        Roles = new List<string>(),
                        Route = $"/page/{pageId}",
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
                    Icon = page.Icon,
                    Names = page.PageTranslations.ToDictionary(t => t.LanguageCode, t => t.Name),
                    IsVisible = page.IsVisible,
                    Roles = page.Roles?.Split(',').Where(r => !string.IsNullOrWhiteSpace(r)).ToList() ?? new List<string>(),
                    Route = page.Route,
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

                // Mise à jour des propriétés de la page
                page.Icon = layout.Icon;
                page.IsVisible = layout.IsVisible;
                page.Route = layout.Route;
                page.Roles = string.Join(",", layout.Roles ?? new List<string>());

                // Mise à jour des traductions
                page.PageTranslations.Clear();
                foreach (var translation in layout.Names)
                {
                    page.PageTranslations.Add(new PageTranslation
                    {
                        PageId = pageId,
                        LanguageCode = translation.Key,
                        Name = translation.Value
                    });
                }

                await pageRepository.UpdateAsync(pageId, page);
                logger.LogInformation("Updated page {PageId} basic properties", pageId);

                // Mise à jour des rows et cards
                var existingRows = await rowRepository.GetByPageIdAsync(pageId);
                foreach (var row in existingRows)
                {
                    try
                    {
                        await rowRepository.DeleteAsync(row.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error deleting row {RowId} during layout update for page {PageId}", row.Id, pageId);
                        throw;
                    }
                }

                foreach (var rowResponse in layout.Rows)
                {
                    try
                    {
                        var newRow = new Row
                        {
                            PageId = pageId,
                            Order = rowResponse.Order,
                            Height = rowResponse.Height,
                        };

                        var savedRow = await rowRepository.CreateAsync(newRow);

                        foreach (var cardDto in rowResponse.Cards)
                        {
                            await cardService.CreateAsync(savedRow.Id, cardDto);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error creating row and cards during layout update for page {PageId}", pageId);
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