using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for analyzing a database query before execution
    /// </summary>
    public class DBConnectionAnalyzeQueryDto
    {
        /// <summary>
        /// The SQL query text to analyze
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Dictionary of parameters used in the query, where key is the parameter name
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }
}