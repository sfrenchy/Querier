using System;
using System.Collections.Generic;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;
using System.Linq;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for database connection information
    /// </summary>
    public class DBConnectionDto
    {
        /// <summary>
        /// Unique identifier of the database connection
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Type of database connection (e.g., SQL Server, PostgreSQL)
        /// </summary>
        public DbConnectionType ConnectionType { get; set; }

        /// <summary>
        /// Display name of the database connection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// API route associated with this database connection
        /// </summary>
        public string ApiRoute { get; set; }
        
        /// <summary>
        /// Name of the database context
        /// </summary>
        public string ContextName { get; set; }
        
        public string Description { get; set; }
        public List<DBConnectionStringParameterDto> Parameters { get; set; }
            
        public byte[] AssemblyDll { get; set; }
        public byte[] AssemblyPdb { get; set; }
        public byte[] AssemblySourceZip { get; set; }
        /// <summary>
        /// Creates a DBConnectionDto from a domain entity
        /// </summary>
        /// <param name="connection">The domain entity to convert</param>
        /// <returns>A new DBConnectionDto instance</returns>
        public static DBConnectionDto FromEntity(DBConnection connection)
        {
            return new DBConnectionDto
            {
                Id = connection.Id,
                Name = connection.Name,
                ConnectionType = connection.ConnectionType,
                Parameters = connection.Parameters.Select(p => DBConnectionStringParameterDto.FromEntity(p)).ToList(),
                ApiRoute = connection.ApiRoute,
                ContextName = connection.ContextName,
                Description = connection.Description,
                AssemblyDll = connection.AssemblyDll,
                AssemblyPdb = connection.AssemblyPdb,
                AssemblySourceZip = connection.AssemblySourceZip
            };
        }
    }
} 