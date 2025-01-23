namespace Querier.Api.Domain.Entities.Menu
{
    public class MenuTranslation
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public string LanguageCode { get; set; }
        public string Name { get; set; }

        public virtual Menu Menu { get; set; }
    }
} 