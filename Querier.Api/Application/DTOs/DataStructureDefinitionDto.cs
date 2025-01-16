using System.Collections.Generic;

namespace Querier.Api.Application.DTOs;

/// <summary>
/// Unified DTO for describing data structures, whether they come from Entity Framework or API endpoints
/// </summary>
public class DataStructureDefinitionDto
{
    /// <summary>
    /// Name of the data structure
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Description of the data structure
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// JSON Schema definition for the data structure.
    /// For Entity Framework entities, this will be auto-generated from the entity properties.
    /// For API endpoints, this will be provided by the endpoint metadata.
    /// </summary>
    public string JsonSchema { get; set; }

    /// <summary>
    /// The type of the data structure (e.g., 'object', 'array')
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The source type of the data structure (Entity, Api)
    /// </summary>
    public DataSourceType SourceType { get; set; }

    /// <summary>
    /// HTTP status code for API responses. Only relevant when SourceType is Api.
    /// </summary>
    public int? StatusCode { get; set; }
}

/// <summary>
/// Defines the type of data source
/// </summary>
public enum DataSourceType
{
    /// <summary>
    /// Data comes from Entity Framework
    /// </summary>
    Entity,

    /// <summary>
    /// Data comes from an API endpoint
    /// </summary>
    Api
} 