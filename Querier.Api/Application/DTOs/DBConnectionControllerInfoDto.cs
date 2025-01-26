namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing information about a database connection controller
    /// </summary>
    public class DBConnectionControllerInfoDto
    {
        /// <summary>
        /// Name of the controller
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Base route for the controller's endpoints
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// JSON Schema definition for the GET response structure
        /// </summary>
        public string ResponseEntityJsonSchema { get; set; }
        
        public string ParameterJsonSchema { get; set; }
    }
} 