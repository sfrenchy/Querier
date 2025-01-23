using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu
{
    public class Menu
    {
        public Menu()
        {
            Translations = new HashSet<MenuTranslation>();
        }

        public int Id { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public string Roles { get; set; }
        public string Route { get; set; }

        public virtual ICollection<MenuTranslation> Translations { get; set; }
        public virtual ICollection<Page> Pages { get; set; }
    }
} 