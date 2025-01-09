using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new database connection
    /// </summary>
    public class DBConnectionCreateDto
    {
        /// <summary>
        /// Type of database connection to create (e.g., SQL Server, PostgreSQL)
        /// </summary>
        public DbConnectionType ConnectionType { get; set; }

        /// <summary>
        /// Display name for the new database connection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Connection string for connecting to the database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// API route for accessing this database context
        /// </summary>
        public string ContextApiRoute { get; set; }

        /// <summary>
        /// Indicates whether to automatically generate controllers and services for stored procedures
        /// </summary>
        public bool GenerateProcedureControllersAndServices { get; set; } = true;
    }
} 