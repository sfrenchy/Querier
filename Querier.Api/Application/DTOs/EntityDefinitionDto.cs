using System.Collections.Generic;

namespace Querier.Api.Application.DTOs;

public class EntityDefinitionDto
{
    /// <summary>
    /// The name of the entity
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The properties of the entity
    /// </summary>
    public List<PropertyDefinitionDto> Properties { get; set; }
}