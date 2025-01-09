using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu;

public class Page
{
    public int Id { get; set; }
    public string Icon { get; set; }
    public int Order { get; set; }
    public bool IsVisible { get; set; }
    public string Roles { get; set; }
    public string Route { get; set; }
    public int DynamicMenuCategoryId { get; set; }
    public virtual Menu Menu { get; set; }
    public virtual ICollection<PageTranslation> PageTranslations { get; set; }
    public virtual ICollection<Row> Rows { get; set; }

    public Page()
    {
        PageTranslations = new HashSet<PageTranslation>();
        Rows = new HashSet<Row>();
    }
}