using System.Collections.Generic;
using Querier.Api.Application.DTOs.Menu.Responses;

namespace Querier.Api.Application.DTOs.Menu.Responses
{
    public class LayoutResponse
    {
        public int PageId { get; set; }
        public string Icon { get; set; }
        public Dictionary<string, string> Names { get; set; }
        public bool IsVisible { get; set; }
        public List<string> Roles { get; set; }
        public string Route { get; set; }
        public List<DynamicRowResponse> Rows { get; set; }
    }
} 