namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing information about a database endpoint's response
    /// </summary>
    public class DBConnectionEndpointResponseInfoDto
    {
        /// <summary>
        /// HTTP status code for the response (e.g., 200, 400, 404, 500)
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Data type of the response content (e.g., 'object', 'array', 'string')
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Description of the response and when it occurs
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// JSON Schema definition for the response structure
        /// </summary>
        public string JsonSchema { get; set; }
    }
} 