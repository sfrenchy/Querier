using System.Collections.Generic;

namespace Querier.Api.Application.DTOs.Requests.Page
{
    public class CreatePageRequest
    {
        public Dictionary<string, string> Names { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; }
        public List<string> Roles { get; set; }
        public string Route { get; set; }
        public int DynamicMenuCategoryId { get; set; }
    }
} 