using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu
{
    public class Page
    {
        public int Id { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public string Roles { get; set; }  // Stocké comme string avec séparateur ','
        public string Route { get; set; }
        public int MenuCategoryId { get; set; }
        public virtual MenuCategory MenuCategory { get; set; }
        public virtual ICollection<PageTranslation> Translations { get; set; }
        public virtual ICollection<DynamicRow> Rows { get; set; }

        public Page()
        {
            Translations = new HashSet<PageTranslation>();
            Rows = new HashSet<DynamicRow>();
        }
    }
}