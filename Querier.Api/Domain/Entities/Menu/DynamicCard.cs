using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Menu
{
    public class DynamicCard
    {
        public DynamicCard()
        {
            Translations = new HashSet<DynamicCardTranslation>();
            GridWidth = 12;
            BackgroundColor = 0xFF000000; // Noir
            TextColor = 0xFFFFFFFF; // Blanc
        }

        public int Id { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public int GridWidth { get; set; }
        public string Configuration { get; set; }
        public int DynamicRowId { get; set; }
        public uint? BackgroundColor { get; set; }
        public uint? TextColor { get; set; }
        public uint? HeaderBackgroundColor { get; set; }
        public uint? HeaderTextColor { get; set; }

        
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