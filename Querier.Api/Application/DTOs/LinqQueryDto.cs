using System;
using System.Collections.Generic;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities;

namespace Querier.Api.Application.DTOs;

public class LinqQueryDto
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
    /// The actual Linq query text
    /// </summary>
    public string Query { get; set; }

    /// <summary>
    /// Identifier of the user who created the query
    /// </summary>
    public string CreatedBy { get; set; }
    public string CreatedByEmail { get; set; }
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
    public byte[] AssemblyDll { get; set; }
    public byte[] AssemblyPdb { get; set; }
    public static LinqQueryDto FromEntity(LinqQuery linqQuery)
    {
        return new LinqQueryDto()
        {
            Id = linqQuery.Id,
            Name = linqQuery.Name,
            Description = linqQuery.Description,
            Query = linqQuery.Query,
            CreatedBy = linqQuery.CreatedBy,
            CreatedAt = linqQuery.CreatedAt,
            LastModifiedAt = linqQuery.LastModifiedAt,
            IsPublic = linqQuery.IsPublic,
            Parameters = linqQuery.Parameters,
            DBConnection = new DBConnectionDto()
            {
                Id = linqQuery.ConnectionId,
                Name = linqQuery.Connection.Name,
                ConnectionType = Enum.Parse<DbConnectionType>(linqQuery.Connection.ConnectionType.ToString()),
                ApiRoute = linqQuery.Connection.ApiRoute
            },
            DBConnectionId = linqQuery.ConnectionId,
            OutputDescription = linqQuery.OutputDescription,
            AssemblyDll = linqQuery.AssemblyDll,
            AssemblyPdb = linqQuery.AssemblyPdb
        };
    }
}