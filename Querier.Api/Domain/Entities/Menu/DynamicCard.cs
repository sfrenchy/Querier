namespace Querier.Api.Domain.Entities.Menu
{
    public class DynamicCard
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Order { get; set; }
        public CardType Type { get; set; }
        public bool IsResizable { get; set; }
        public bool IsCollapsible { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public string Configuration { get; set; }  // JSON configuration spécifique au type
        public int DynamicRowId { get; set; }
        
        public virtual DynamicRow Row { get; set; }
    }

    public enum CardType
    {
        Table,
        Chart,
        Metrics,
        Form,
        Custom
    }
} 