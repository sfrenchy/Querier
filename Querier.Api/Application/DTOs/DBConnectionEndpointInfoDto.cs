using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing information about a database connection endpoint
    /// </summary>
    public class DBConnectionEndpointInfoDto
    {
        /// <summary>
        /// Name of the controller handling the endpoint
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// Name of the action method within the controller
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// URL route pattern for the endpoint
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// HTTP method used by the endpoint (GET, POST, PUT, DELETE, etc.)
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// Description of what the endpoint does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List of parameters accepted by the endpoint
        /// </summary>
        public List<DBConnectionEndpointRequestInfoDto> Parameters { get; set; } = new();

        /// <summary>
        /// List of possible responses returned by the endpoint
        /// </summary>
        public List<DBConnectionEndpointResponseInfoDto> Responses { get; set; } = new();
    }
}