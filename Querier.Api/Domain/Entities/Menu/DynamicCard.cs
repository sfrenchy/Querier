using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu
{
    public class DynamicCard
    {
        public DynamicCard()
        {
            Translations = new HashSet<DynamicCardTranslation>();
            UseAvailableWidth = true;
            UseAvailableHeight = true;
            BackgroundColor = 0xFF000000; // Noir
            TextColor = 0xFFFFFFFF; // Blanc
        }

        public int Id { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public bool IsResizable { get; set; }
        public bool IsCollapsible { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public string Configuration { get; set; }
        public int DynamicRowId { get; set; }
        public bool UseAvailableWidth { get; set; }
        public bool UseAvailableHeight { get; set; }
        public int? BackgroundColor { get; set; }
        public int? TextColor { get; set; }
        
        public virtual DynamicRow Row { get; set; }
        public virtual ICollection<DynamicCardTranslation> Translations { get; set; }
    }

    public class DynamicCardTranslation
    {
        public int Id { get; set; }
        public int DynamicCardId { get; set; }
        public string LanguageCode { get; set; }
        public string Title { get; set; }
        public virtual DynamicCard Card { get; set; }
    }
} 