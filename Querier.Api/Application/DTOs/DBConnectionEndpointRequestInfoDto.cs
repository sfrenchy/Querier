namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing information about a database endpoint's request parameter
    /// </summary>
    public class DBConnectionEndpointRequestInfoDto
    {
        /// <summary>
        /// Name of the parameter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data type of the parameter (e.g., string, int, object)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Description of the parameter's purpose and usage
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Indicates whether the parameter is required for the request
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Source of the parameter (e.g., 'query', 'body', 'route', 'header')
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// JSON Schema definition for complex parameter types
        /// </summary>
        public string JsonSchema { get; set; }
    }
}