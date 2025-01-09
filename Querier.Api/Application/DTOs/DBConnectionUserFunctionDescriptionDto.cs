using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing the description of a user-defined database function
    /// </summary>
    public class DBConnectionUserFunctionDescriptionDto
    {
        /// <summary>
        /// Name of the user-defined function
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Schema containing the user-defined function
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// List of parameters accepted by the user-defined function
        /// </summary>
        public List<DBConnectionEndpointParameterDescriptionDto> Parameters { get; set; } = new();

        /// <summary>
        /// Data type returned by the user-defined function
        /// </summary>
        public string ReturnType { get; set; }
    }
}