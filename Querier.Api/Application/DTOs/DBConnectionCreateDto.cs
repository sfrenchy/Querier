using System.Collections.Generic;
using System.Linq;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Infrastructure.Services;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new database connection
    /// </summary>
    public class DBConnectionCreateDto
    {
        /// <summary>
        /// Display name for the new database connection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of database connection to create (e.g., SQL Server, PostgreSQL)
        /// </summary>
        public DbConnectionType ConnectionType { get; set; }

        /// <summary>
        /// Collection of connection string parameters for the new database connection
        /// </summary>
        public ICollection<ConnectionStringParameterCreateDto> Parameters { get; set; }

        /// <summary>
        /// Context name for the new database connection
        /// </summary>
        public string ContextName { get; set; }

        /// <summary>
        /// API route for accessing this database context
        /// </summary>
        public string ApiRoute { get; set; }

        /// <summary>
        /// Indicates whether to automatically generate controllers and services for stored procedures
        /// </summary>
        public bool GenerateProcedureControllersAndServices { get; set; } = true;

        public DBConnection ToEntity(IEncryptionService encryptionService)
        {
            return new DBConnection
            {
                Name = Name,
                ConnectionType = ConnectionType,
                ApiRoute = ApiRoute,
                ContextName = ContextName,
                Parameters = Parameters.Select(p => new ConnectionStringParameter
                {
                    Key = p.Key,
                    IsEncrypted = p.IsEncrypted,
                    EncryptionService = encryptionService
                }).ToList()
            };
        }
    }

    public class ConnectionStringParameterCreateDto
    {
        /// <summary>
        /// Key for the connection string parameter
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Value for the connection string parameter
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Indicates whether the connection string parameter is encrypted
        /// </summary>
        public bool IsEncrypted { get; set; }
    }
} 