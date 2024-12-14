namespace Querier.Api.Domain.Entities.Menu
{
    public class DynamicMenuCategoryTranslation
    {
        public int Id { get; set; }
        public int MenuCategoryId { get; set; }
        public string LanguageCode { get; set; }
        public string Name { get; set; }

        public virtual DynamicMenuCategory MenuCategory { get; set; }
    }
} 