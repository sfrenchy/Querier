using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new SQL query with sample parameters
    /// </summary>
    public class SQLQueryCreateDto
    {
        /// <summary>
        /// The SQL query information to create
        /// </summary>
        public SQLQueryDTO Query { get; set; }

        /// <summary>
        /// Dictionary of sample parameter values for testing the query, where key is the parameter name
        /// </summary>
        public Dictionary<string, object> SampleParameters { get; set; }
    }
}