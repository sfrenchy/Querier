using System.Collections.Generic;

namespace Querier.Api.Application.DTOs.Menu.Responses
{
    public class DynamicCardResponse
    {
        public int Id { get; set; }
        public Dictionary<string, string> Titles { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public int GridWidth { get; set; }
        public object Configuration { get; set; }
        public uint? BackgroundColor { get; set; }
        public uint? TextColor { get; set; }
        public uint? HeaderBackgroundColor { get; set; }
        public uint? HeaderTextColor { get; set; }
    }
} 