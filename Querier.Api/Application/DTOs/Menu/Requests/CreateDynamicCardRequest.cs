using System.Collections.Generic;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.DTOs.Menu.Requests
{
    public class CreateDynamicCardRequest
    {
        public Dictionary<string, string> Titles { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public bool IsResizable { get; set; }
        public bool IsCollapsible { get; set; }
        public double? Height { get; set; }
        public double? Width { get; set; }
        public object Configuration { get; set; }
        public bool UseAvailableWidth { get; set; }
    }
} 