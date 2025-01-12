namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing the description of a parameter for a generated controller endpoint
    /// Used to describe complex parameter types in the API
    /// </summary>
    public class DBConnectionParameterDescriptionDto
    {
        /// <summary>
        /// Name of the parameter in the endpoint
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Complex data type of the parameter with its full structure
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Parameter binding mode in the endpoint
        /// </summary>
        public string Mode { get; set; }
    }
} 