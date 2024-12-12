namespace Querier.Api.Domain.Entities.Menu
{
    public class PageTranslation
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string LanguageCode { get; set; }
        public string Name { get; set; }
        public virtual Page Page { get; set; }
    }
} 