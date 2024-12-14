using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu
{
    public class DynamicMenuCategory
    {
        public DynamicMenuCategory()
        {
            Translations = new HashSet<DynamicMenuCategoryTranslation>();
        }

        public int Id { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public string Roles { get; set; }
        public string Route { get; set; }

        public virtual ICollection<DynamicMenuCategoryTranslation> Translations { get; set; }
        public virtual ICollection<DynamicPage> Pages { get; set; }
    }
} 