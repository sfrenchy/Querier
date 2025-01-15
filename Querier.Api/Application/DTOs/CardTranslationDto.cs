using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.DTOs
{
    public class CardTranslationDto
    {
        public string LanguageCode { get; set; }
        public string Title { get; set; }

        public static TranslatableStringDto FromEntity(CardTranslation entity)
        {
            return new()
            {
                LanguageCode = entity.LanguageCode,
                Value = entity.Title
            };
        }

        public static CardTranslation ToEntity(TranslatableStringDto dto)
        {
            return new()
            {
                LanguageCode = dto.LanguageCode,
                Title = dto.Value
            };
        }
    }
}