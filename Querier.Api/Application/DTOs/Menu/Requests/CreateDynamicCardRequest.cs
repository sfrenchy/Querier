using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.DTOs.Menu.Requests
{
    public class CreateDynamicCardRequest
    {
        public string Title { get; set; }
        public int Order { get; set; }
        public CardType Type { get; set; }
        public bool IsResizable { get; set; }
        public bool IsCollapsible { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public object Configuration { get; set; }  // Sera sérialisé en JSON
    }
} 