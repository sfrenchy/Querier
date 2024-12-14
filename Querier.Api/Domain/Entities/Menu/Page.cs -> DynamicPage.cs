using System.Collections.Generic;
using Querier.Api.Domain.Entities.Menu;

public class DynamicPage
{
    public int Id { get; set; }
    public string Icon { get; set; }
    public int Order { get; set; }
    public bool IsVisible { get; set; }
    public string Roles { get; set; }
    public string Route { get; set; }
    public int DynamicMenuCategoryId { get; set; }
    public virtual DynamicMenuCategory DynamicMenuCategory { get; set; }
    public virtual ICollection<DynamicPageTranslation> DynamicPageTranslations { get; set; }
    public virtual ICollection<DynamicRow> Rows { get; set; }

    public DynamicPage()
    {
        DynamicPageTranslations = new HashSet<DynamicPageTranslation>();
        Rows = new HashSet<DynamicRow>();
    }
} 