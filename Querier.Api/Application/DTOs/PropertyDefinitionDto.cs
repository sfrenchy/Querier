using System.Collections.Generic;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Application.DTOs;

public class PropertyDefinitionDto
{
    /// <summary>
    /// The list of available items
    /// </summary>
    private List<PropertyItemDefinitionDto> _availableItems;

    /// <summary>
    /// The name of the property
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The CSharp type of the property (as string)
    /// </summary>
    public string Type { get; set; } = "string?";

    /// <summary>
    /// List of property options
    /// </summary>
    public List<PropertyOption> Options { get; set; } = new List<PropertyOption>() { PropertyOption.IsNullable };

    /// <summary>
    /// Custom getter and setter for the available items of the property
    /// </summary>
    public List<PropertyItemDefinitionDto> AvailableItems
    {
        get
        {
            if (_availableItems == null)
            {
                // Do what you have to do to get available items from database... :(
            }
            return _availableItems;
        }
        set
        {
            _availableItems = value;
        }
    }
}