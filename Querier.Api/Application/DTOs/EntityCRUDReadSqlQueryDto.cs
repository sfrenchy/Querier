using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for executing read-only SQL queries with optional filters
    /// </summary>
    public class EntityCRUDReadSqlQueryDto
    {
        /// <summary>
        /// The fully qualified name of the DbContext type on the server
        /// Determines which database the query will be executed against
        /// </summary>
        public string ContextTypeName { get; set; }

        /// <summary>
        /// The SQL query to execute for reading data
        /// Should be a SELECT statement and carefully validated to prevent SQL injection
        /// </summary>
        public string SqlQuery { get; set; }

        /// <summary>
        /// List of filters to apply to the query results
        /// Filters are applied after the query execution
        /// </summary>
        public List<DataFilterDto> Filters { get; set; }
    }
}