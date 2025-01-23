using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for updating an existing SQL query
    /// </summary>
    public class SQLQueryUpdateDto
    {
        /// <summary>
        /// Unique identifier of the SQL query to update
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Updated SQL query information
        /// </summary>
        public SqlQueryDto Query { get; set; }

        /// <summary>
        /// Dictionary of updated sample parameter values for testing the query, where key is the parameter name
        /// </summary>
        public Dictionary<string, object> SampleParameters { get; set; }
    }
}