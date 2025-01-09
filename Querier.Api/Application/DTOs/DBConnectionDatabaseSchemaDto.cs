using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object representing the complete schema of a database
    /// </summary>
    public class DBConnectionDatabaseSchemaDto
    {
        /// <summary>
        /// List of all tables in the database with their descriptions
        /// </summary>
        public List<DBConnectionTableDescriptionDto> Tables { get; set; } = new();

        /// <summary>
        /// List of all views in the database with their descriptions
        /// </summary>
        public List<DBConnectionViewDescriptionDto> Views { get; set; } = new();

        /// <summary>
        /// List of all stored procedures in the database with their descriptions
        /// </summary>
        public List<DBConnectionStoredProcedureDescriptionDto> StoredProcedures { get; set; } = new();

        /// <summary>
        /// List of all user-defined functions in the database with their descriptions
        /// </summary>
        public List<DBConnectionUserFunctionDescriptionDto> UserFunctions { get; set; } = new();
    }
}