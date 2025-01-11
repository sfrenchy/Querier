using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.DTOs
{
    public class CardTranslationDto
    {
        public string LanguageCode { get; set; }
        public string Title { get; set; }

        public static CardTranslationDto FromEntity(CardTranslation entity)
        {
            return new()
            {
                LanguageCode = entity.LanguageCode,
                Title = entity.Title
            };
        }

        public static CardTranslation ToEntity(CardTranslationDto dto)
        {
            return new()
            {
                LanguageCode = dto.LanguageCode,
                Title = dto.Title
            };
        }
    }
}