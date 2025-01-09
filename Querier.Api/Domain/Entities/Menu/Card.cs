using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu;

public class Card
{
    public Card()
    {
        CardTranslations = new HashSet<CardTranslation>();
        GridWidth = 12;
        BackgroundColor = 0xFF000000; // Noir
        TextColor = 0xFFFFFFFF; // Blanc
    }

    public int Id { get; set; }
    public int Order { get; set; }
    public string Type { get; set; }
    public int GridWidth { get; set; }
    public string Configuration { get; set; }
    public int RowId { get; set; }
    public uint? BackgroundColor { get; set; }
    public uint? TextColor { get; set; }
    public uint? HeaderBackgroundColor { get; set; }
    public uint? HeaderTextColor { get; set; }

        
    public virtual Row Row { get; set; }
    public virtual ICollection<CardTranslation> CardTranslations { get; set; }
}