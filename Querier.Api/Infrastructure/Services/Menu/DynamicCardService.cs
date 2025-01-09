using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Application.Interfaces.Services.Menu;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services.Menu
{
    public class DynamicCardService : IDynamicCardService
    {
        private readonly IDynamicCardRepository _repository;

        public DynamicCardService(IDynamicCardRepository repository)
        {
            _repository = repository;
        }

        public async Task<CardDto> GetByIdAsync(int id)
        {
            var card = await _repository.GetByIdAsync(id);
            return card != null ? MapToResponse(card) : null;
        }

        public async Task<IEnumerable<CardDto>> GetByRowIdAsync(int rowId)
        {
            var cards = await _repository.GetByRowIdAsync(rowId);
            return cards.Select(MapToResponse);
        }

        public async Task<CardDto> CreateAsync(int rowId, CardDto request)
        {
            var order = await _repository.GetMaxOrderInRowAsync(rowId) + 1;
            
            var card = new Card
            {
                Order = order,
                Type = request.Type,
                Configuration = request.Configuration != null 
                    ? JsonConvert.SerializeObject(request.Configuration)
                    : null,
                RowId = rowId,
                GridWidth = 12,
                BackgroundColor = request.BackgroundColor ?? 0xFF000000,
                TextColor = request.TextColor ?? 0xFFFFFFFF,
            };

            var result = await _repository.CreateAsync(card);
            return MapToResponse(result);
        }

        public async Task<CardDto> UpdateAsync(int id, CardDto request)
        {
            var existingCard = await _repository.GetByIdAsync(id);
            if (existingCard == null) return null;

            
            existingCard.Type = request.Type;
            existingCard.GridWidth = request.GridWidth;
            existingCard.Order = request.Order;
            existingCard.BackgroundColor = request.BackgroundColor;
            existingCard.TextColor = request.TextColor;

            existingCard.CardTranslations.Clear();
            foreach (var translation in request.Titles)
            {
                existingCard.CardTranslations.Add(new CardTranslation
                {
                    LanguageCode = translation.Key,
                    Title = translation.Value,
                    DynamicCardId = id
                });
            }

            existingCard.Configuration = request.Configuration != null 
                ? JsonConvert.SerializeObject(request.Configuration)
                : null;

            var result = await _repository.UpdateAsync(existingCard);
            return MapToResponse(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        public async Task<bool> ReorderAsync(int rowId, List<int> cardIds)
        {
            var cards = await _repository.GetByRowIdAsync(rowId);
            var cardDict = cards.ToDictionary(c => c.Id);

            for (int i = 0; i < cardIds.Count; i++)
            {
                if (cardDict.TryGetValue(cardIds[i], out var card))
                {
                    card.Order = i + 1;
                    await _repository.UpdateAsync(card);
                }
            }

            return true;
        }

        private static CardDto MapToResponse(Card card)
        {
            return new CardDto
            {
                Id = card.Id,
                Titles = card.CardTranslations.ToDictionary(x => x.LanguageCode, x => x.Title),
                Order = card.Order,
                Type = card.Type.ToString(),
                GridWidth = card.GridWidth,
                Configuration = card.Configuration != null 
                    ? JsonConvert.DeserializeObject(card.Configuration)
                    : null,
                TextColor = card.TextColor,
                BackgroundColor = card.BackgroundColor,
                HeaderBackgroundColor = card.HeaderBackgroundColor,
                HeaderTextColor = card.HeaderTextColor
            };
        }
    }
} 