using System.Collections.Generic;

namespace Querier.Api.Application.DTOs.Responses.DBConnection
{
    public class EndpointInfoResponse
    {
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Route { get; set; }
        public string HttpMethod { get; set; }
        public string Description { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new();
        public List<ResponseInfo> Responses { get; set; } = new();
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public string Source { get; set; }
        public string JsonSchema { get; set; }
    }

    public class ResponseInfo
    {
        public int StatusCode { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string JsonSchema { get; set; }
    }
} 