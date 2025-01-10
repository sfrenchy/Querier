using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services
{
    public class LayoutService(
        IPageRepository pageRepository,
        IRowRepository rowRepository,
        ICardService cardService)
        : ILayoutService
    {
        public async Task<LayoutDto> GetLayoutAsync(int pageId)
        {
            var page = await pageRepository.GetByIdAsync(pageId);
            if (page == null)
            {
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
                var cards = await cardService.GetByRowIdAsync(row.Id);

                rowResponses.Add(new RowDto
                {
                    Id = row.Id,
                    Order = row.Order,
                    Height = row.Height,
                    Cards = cards
                });
            }

            return new LayoutDto
            {
                PageId = page.Id,
                Icon = page.Icon,
                Names = page.PageTranslations.ToDictionary(t => t.LanguageCode, t => t.Name),
                IsVisible = page.IsVisible,
                Roles = page.Roles?.Split(',').ToList() ?? new List<string>(),
                Route = page.Route,
                Rows = rowResponses
            };
        }

        public async Task<LayoutDto> UpdateLayoutAsync(int pageId, LayoutDto layout)
        {
            var page = await pageRepository.GetByIdAsync(pageId);
            if (page == null) return null;

            // Mise à jour des propriétés de la page
            page.Icon = layout.Icon;
            page.IsVisible = layout.IsVisible;
            page.Route = layout.Route;
            page.Roles = string.Join(",", layout.Roles);

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

            // Mise à jour des rows et cards
            var existingRows = await rowRepository.GetByPageIdAsync(pageId);
            foreach (var row in existingRows)
            {
                await rowRepository.DeleteAsync(row.Id);
            }

            foreach (var rowResponse in layout.Rows)
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

            return await GetLayoutAsync(pageId);
        }

        public async Task<bool> DeleteLayoutAsync(int pageId)
        {
            return await pageRepository.DeleteAsync(pageId);
        }
    }
} 