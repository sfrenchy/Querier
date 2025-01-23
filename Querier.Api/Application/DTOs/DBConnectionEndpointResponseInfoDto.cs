namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing information about a database endpoint's response
    /// </summary>
    public class DBConnectionEndpointResponseInfoDto : DataStructureDefinitionDto
    {
        /// <summary>
        /// HTTP status code for the response (e.g., 200, 400, 404, 500)
        /// </summary>
        public int StatusCode { get; set; }
    }
} 