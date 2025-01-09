using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing the analysis results of a database query
    /// </summary>
    public class DBConnectionQueryAnalysisDto
    {
        /// <summary>
        /// List of database tables referenced in the query
        /// </summary>
        public List<string> Tables { get; set; } = new();

        /// <summary>
        /// List of database views referenced in the query
        /// </summary>
        public List<string> Views { get; set; } = new();

        /// <summary>
        /// List of stored procedures referenced in the query
        /// </summary>
        public List<string> StoredProcedures { get; set; } = new();

        /// <summary>
        /// List of user-defined functions referenced in the query
        /// </summary>
        public List<string> UserFunctions { get; set; } = new();
    }
}