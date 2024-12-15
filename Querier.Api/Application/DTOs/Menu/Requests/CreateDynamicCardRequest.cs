using System.Collections.Generic;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.DTOs.Menu.Requests
{
    public class CreateDynamicCardRequest
    {
        public Dictionary<string, string> Titles { get; set; }
        public int Order { get; set; }
        public string Type { get; set; }
        public int GridWidth { get; set; }
        public object Configuration { get; set; }
        public uint? BackgroundColor { get; set; }
        public uint? TextColor { get; set; }
    }
} 