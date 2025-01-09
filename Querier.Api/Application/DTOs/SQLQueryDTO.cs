using System;
using System.Collections.Generic;
using Querier.Api.Domain.Entities;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for SQL query information and metadata
    /// </summary>
    public class SQLQueryDTO
    {
        /// <summary>
        /// Unique identifier of the SQL query
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Name of the SQL query
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of what the SQL query does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The actual SQL query text
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Identifier of the user who created the query
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Date and time when the query was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the query was last modified
        /// </summary>
        public DateTime? LastModifiedAt { get; set; }

        /// <summary>
        /// Indicates whether the query is publicly accessible
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Dictionary of parameters used in the query, where key is the parameter name
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// ID of the database connection this query is associated with
        /// </summary>
        public int DBConnectionId { get; set; }

        /// <summary>
        /// Database connection details for this query
        /// </summary>
        public DBConnectionDto DBConnection { get; set; }

        /// <summary>
        /// Description of the query's output format and structure
        /// </summary>
        public string? OutputDescription { get; set; }

        /// <summary>
        /// Creates a SQLQueryDTO from a domain entity
        /// </summary>
        /// <param name="sqlQuery">The domain entity to convert</param>
        /// <returns>A new SQLQueryDTO instance</returns>
        public static SQLQueryDTO FromEntity(SQLQuery sqlQuery)
        {
            return new SQLQueryDTO()
            {
                Id = sqlQuery.Id,
                Name = sqlQuery.Name,
                Description = sqlQuery.Description,
                Query = sqlQuery.Query,
                CreatedBy = sqlQuery.CreatedBy,
                CreatedAt = sqlQuery.CreatedAt,
                LastModifiedAt = sqlQuery.LastModifiedAt,
                IsPublic = sqlQuery.IsPublic,
                Parameters = sqlQuery.Parameters,
                DBConnection = new DBConnectionDto()
                {
                    Name = sqlQuery.Connection.Name,
                    ConnectionString = sqlQuery.Connection.ConnectionString,
                    ConnectionType = sqlQuery.Connection.ConnectionType.ToString(),
                    ApiRoute = sqlQuery.Connection.ApiRoute
                },
                OutputDescription = sqlQuery.OutputDescription
            };
        }
    }
} 