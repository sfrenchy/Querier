namespace Querier.Api.Domain.Entities.Menu
{
    public class MenuCategoryTranslation
    {
        public int Id { get; set; }
        public int MenuCategoryId { get; set; }
        public string LanguageCode { get; set; }
        public string Name { get; set; }

        public virtual MenuCategory MenuCategory { get; set; }
    }
} 