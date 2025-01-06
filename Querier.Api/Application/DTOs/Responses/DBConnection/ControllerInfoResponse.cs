using System.Collections.Generic;

namespace Querier.Api.Application.DTOs.Responses.DBConnection
{
    public class ControllerInfoResponse
    {
        public string Name { get; set; }
        public string Route { get; set; }
        public string HttpGetJsonSchema { get; set; }
    }
} 