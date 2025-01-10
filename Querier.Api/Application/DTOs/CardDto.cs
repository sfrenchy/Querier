using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for a card component in the page layout
    /// </summary>
    public class CardDto
    {
        /// <summary>
        /// Unique identifier of the card
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Dictionary of localized titles for the card, where key is the language code
        /// </summary>
        public IEnumerable<CardTranslationDto> Titles { get; set; }

        /// <summary>
        /// Display order of the card within its row
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Type of card component (e.g., 'chart', 'table', 'text')
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Width of the card in grid units
        /// </summary>
        public int GridWidth { get; set; }

        /// <summary>
        /// Card-specific configuration object based on the card type
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Optional background color of the card body (in RGBA format)
        /// </summary>
        public uint? BackgroundColor { get; set; }

        /// <summary>
        /// Optional text color of the card body (in RGBA format)
        /// </summary>
        public uint? TextColor { get; set; }

        /// <summary>
        /// Optional background color of the card header (in RGBA format)
        /// </summary>
        public uint? HeaderBackgroundColor { get; set; }

        /// <summary>
        /// Optional text color of the card header (in RGBA format)
        /// </summary>
        public uint? HeaderTextColor { get; set; }

        public int RowId { get; set; }
        public static CardDto FromEntity(Card entity)
        {
            return new CardDto()
            {
                Configuration = entity.Configuration != null 
                    ? JsonConvert.DeserializeObject(entity.Configuration)
                    : null,
                GridWidth = entity.GridWidth,
                TextColor = entity.TextColor,
                BackgroundColor = entity.TextColor,
                HeaderTextColor = entity.HeaderTextColor,
                HeaderBackgroundColor = entity.HeaderBackgroundColor,
                Order = entity.Order,
                Type = entity.Type,
                Titles = entity.CardTranslations.Select(CardTranslationDto.FromEntity),
                Id = entity.Id,
                RowId = entity.RowId
            };
        }

        public static Card ToEntity(CardDto dto)
        {
            return new Card()
            {
                Id = dto.Id,
                Configuration = dto.Configuration != null 
                    ? JsonConvert.SerializeObject(dto.Configuration)
                    : null,
                GridWidth = dto.GridWidth,
                TextColor = dto.TextColor,
                BackgroundColor = dto.BackgroundColor,
                HeaderTextColor = dto.HeaderTextColor,
                HeaderBackgroundColor = dto.HeaderBackgroundColor,
                Order = dto.Order,
                Type = dto.Type,
                CardTranslations = dto.Titles.Select(CardTranslationDto.ToEntity).ToList(),
                RowId = dto.RowId
            };
        }
    }
} 