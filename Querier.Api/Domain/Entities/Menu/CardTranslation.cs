namespace Querier.Api.Domain.Entities.Menu
{
    public class CardTranslation
    {
        public int Id { get; set; }
        public int DynamicCardId { get; set; }
        public string LanguageCode { get; set; }
        public string Title { get; set; }
        public virtual Card Card { get; set; }
    }
} 