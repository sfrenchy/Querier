using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Querier.Api.Application.DTOs.Menu.Responses;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Application.Interfaces.Services.Menu;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services.Menu
{
    public class LayoutService : ILayoutService
    {
        private readonly IDynamicPageRepository _pageRepository;
        private readonly IDynamicRowRepository _rowRepository;
        private readonly IDynamicCardRepository _cardRepository;

        public LayoutService(
            IDynamicPageRepository pageRepository,
            IDynamicRowRepository rowRepository,
            IDynamicCardRepository cardRepository)
        {
            _pageRepository = pageRepository;
            _rowRepository = rowRepository;
            _cardRepository = cardRepository;
        }

        public async Task<LayoutResponse> GetLayoutAsync(int pageId)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null) return null;

            var rows = await _rowRepository.GetByPageIdAsync(pageId);
            var rowResponses = new List<DynamicRowResponse>();

            foreach (var row in rows)
            {
                var cards = await _cardRepository.GetByRowIdAsync(row.Id);
                var cardResponses = cards.Select(card => new DynamicCardResponse
                {
                    Id = card.Id,
                    Titles = card.Translations.ToDictionary(t => t.LanguageCode, t => t.Title),
                    Order = card.Order,
                    Type = card.Type,
                    IsResizable = card.IsResizable,
                    IsCollapsible = card.IsCollapsible,
                    Height = card.Height,
                    Width = card.Width,
                    Configuration = card.Configuration != null 
                        ? JsonConvert.DeserializeObject(card.Configuration)
                        : null,
                    UseAvailableWidth = card.UseAvailableWidth,
                    UseAvailableHeight = card.UseAvailableHeight,
                    BackgroundColor = card.BackgroundColor,
                    TextColor = card.TextColor,
                }).ToList();

                rowResponses.Add(new DynamicRowResponse
                {
                    Id = row.Id,
                    Order = row.Order,
                    Alignment = row.Alignment.ToString(),
                    CrossAlignment = row.CrossAlignment.ToString(),
                    Spacing = row.Spacing,
                    Cards = cardResponses
                });
            }

            return new LayoutResponse
            {
                PageId = page.Id,
                Icon = page.Icon,
                Names = page.DynamicPageTranslations.ToDictionary(t => t.LanguageCode, t => t.Name),
                IsVisible = page.IsVisible,
                Roles = page.Roles?.Split(',').ToList() ?? new List<string>(),
                Route = page.Route,
                Rows = rowResponses
            };
        }

        public async Task<LayoutResponse> UpdateLayoutAsync(int pageId, LayoutResponse layout)
        {
            var page = await _pageRepository.GetByIdAsync(pageId);
            if (page == null) return null;

            // Mise à jour des propriétés de la page
            page.Icon = layout.Icon;
            page.IsVisible = layout.IsVisible;
            page.Route = layout.Route;
            page.Roles = string.Join(",", layout.Roles);

            // Mise à jour des traductions
            page.DynamicPageTranslations.Clear();
            foreach (var translation in layout.Names)
            {
                page.DynamicPageTranslations.Add(new DynamicPageTranslation
                {
                    DynamicPageId = pageId,
                    LanguageCode = translation.Key,
                    Name = translation.Value
                });
            }

            await _pageRepository.UpdateAsync(pageId, page);

            // Mise à jour des rows et cards
            var existingRows = await _rowRepository.GetByPageIdAsync(pageId);
            foreach (var row in existingRows)
            {
                await _rowRepository.DeleteAsync(row.Id);
            }

            foreach (var rowResponse in layout.Rows)
            {
                var newRow = new DynamicRow
                {
                    PageId = pageId,
                    Order = rowResponse.Order,
                    Alignment = Enum.Parse<MainAxisAlignment>(rowResponse.Alignment),
                    CrossAlignment = Enum.Parse<CrossAxisAlignment>(rowResponse.CrossAlignment),
                    Spacing = rowResponse.Spacing
                };

                var savedRow = await _rowRepository.CreateAsync(newRow);

                foreach (var cardResponse in rowResponse.Cards)
                {
                    var newCard = new DynamicCard
                    {
                        DynamicRowId = savedRow.Id,
                        Order = cardResponse.Order,
                        Type = cardResponse.Type,
                        IsResizable = cardResponse.IsResizable,
                        IsCollapsible = cardResponse.IsCollapsible,
                        Height = cardResponse.Height,
                        Width = cardResponse.Width,
                        Configuration = cardResponse.Configuration != null 
                            ? JsonConvert.SerializeObject(cardResponse.Configuration)
                            : null,
                        UseAvailableWidth = cardResponse.UseAvailableWidth,
                        UseAvailableHeight = cardResponse.UseAvailableHeight,
                        BackgroundColor = cardResponse.BackgroundColor,
                        TextColor = cardResponse.TextColor
                    };

                    foreach (var title in cardResponse.Titles)
                    {
                        newCard.Translations.Add(new DynamicCardTranslation
                        {
                            LanguageCode = title.Key,
                            Title = title.Value
                        });
                    }

                    await _cardRepository.CreateAsync(newCard);
                }
            }

            return await GetLayoutAsync(pageId);
        }

        public async Task<bool> DeleteLayoutAsync(int pageId)
        {
            return await _pageRepository.DeleteAsync(pageId);
        }
    }
} 