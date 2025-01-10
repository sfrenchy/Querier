using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services
{
    public class CardService(ICardRepository repository) : ICardService
    {
        public async Task<CardDto> GetByIdAsync(int id)
        {
            var card = await repository.GetByIdAsync(id);
            return card != null ? CardDto.FromEntity(card) : null;
        }

        public async Task<IEnumerable<CardDto>> GetByRowIdAsync(int rowId)
        {
            var cards = await repository.GetByRowIdAsync(rowId);
            return cards.Select(CardDto.FromEntity);
        }

        public async Task<CardDto> CreateAsync(int rowId, CardDto request)
        {
            var order = await repository.GetMaxOrderInRowAsync(rowId) + 1;
            
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

            var result = await repository.CreateAsync(card);
            return CardDto.FromEntity(result);
        }

        public async Task<CardDto> UpdateAsync(int id, CardDto request)
        {
            var existingCard = await repository.GetByIdAsync(id);
            if (existingCard == null) return null;

            
            existingCard.Type = request.Type;
            existingCard.GridWidth = request.GridWidth;
            existingCard.Order = request.Order;
            existingCard.BackgroundColor = request.BackgroundColor;
            existingCard.TextColor = request.TextColor;
            existingCard.RowId = request.RowId;
            existingCard.Configuration = request.Configuration != null ?
                JsonConvert.SerializeObject(request.Configuration) : null;
            existingCard.HeaderBackgroundColor = request.HeaderBackgroundColor;
            existingCard.HeaderTextColor = request.HeaderTextColor;
            
            existingCard.CardTranslations.Clear();
            foreach (var translation in request.Titles)
            {
                existingCard.CardTranslations.Add(new CardTranslation
                {
                    LanguageCode = translation.LanguageCode,
                    Title = translation.Title,
                    CardId = id
                });
            }

            existingCard.Configuration = request.Configuration != null 
                ? JsonConvert.SerializeObject(request.Configuration)
                : null;

            var result = await repository.UpdateAsync(existingCard);
            return CardDto.FromEntity(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await repository.DeleteAsync(id);
        }

        public async Task<bool> ReorderAsync(int rowId, List<int> cardIds)
        {
            var cards = await repository.GetByRowIdAsync(rowId);
            var cardDict = cards.ToDictionary(c => c.Id);

            for (int i = 0; i < cardIds.Count; i++)
            {
                if (cardDict.TryGetValue(cardIds[i], out var card))
                {
                    card.Order = i + 1;
                    await repository.UpdateAsync(card);
                }
            }

            return true;
        }
    }
} 