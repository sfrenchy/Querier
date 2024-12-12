using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu
{
    public class MenuCategory
    {
        public MenuCategory()
        {
            Translations = new HashSet<MenuCategoryTranslation>();
        }

        public int Id { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public string Roles { get; set; }
        public string Route { get; set; }

        public virtual ICollection<MenuCategoryTranslation> Translations { get; set; }
    }
} 