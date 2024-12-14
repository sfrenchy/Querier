using System.Collections.Generic;

namespace Querier.Api.Application.DTOs.Menu.Responses
{
    public class DynamicCardResponse
    {
        public int Id { get; set; }
        public Dictionary<string, string> Titles { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public bool IsResizable { get; set; }
        public bool IsCollapsible { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public object Configuration { get; set; }
        public bool UseAvailableWidth { get; set; }
        public bool UseAvailableHeight { get; set; }
        public int? BackgroundColor { get; set; }
        public int? TextColor { get; set; }
    }
} 