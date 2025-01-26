using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Castle.Core.Internal;
using Google.Apis.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Attributes;

namespace Querier.Api.Infrastructure.Services;

/// <summary>
/// Service for generating JSON Schema from various sources
/// </summary>
public class JsonSchemaGeneratorService
{
    private readonly ILogger _logger;
    private IModel _efModel;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false
    };

    public JsonSchemaGeneratorService(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a JSON Schema from a .NET Type
    /// </summary>
    public string GenerateFromType(Type type, DbContext dbContext = null)
    {
        try
        {
            _logger.LogDebug("Generating JSON Schema for type {TypeName}", type.Name);
            
            if (dbContext != null)
            {
                _efModel = dbContext.Model;
            }

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["title"] = type.Name,
                ["properties"] = new Dictionary<string, object>()
            };

            var properties = type.GetProperties()
                .Where(p => CustomAttributeExtensions.GetCustomAttribute<NotMappedAttribute>(p) == null);

            var requiredProperties = new List<string>();

            foreach (var prop in properties)
            {
                try
                {
                    var propSchema = GetPropertySchema(prop, type);
                    ((Dictionary<string, object>)schema["properties"])[prop.Name] = propSchema;

                    if (CustomAttributeExtensions.GetCustomAttribute<RequiredAttribute>(prop) != null)
                    {
                        requiredProperties.Add(prop.Name);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error generating schema for type {TypeName}", type.Name);
                    throw;
                }
            }

            if (requiredProperties.Any())
            {
                schema["required"] = requiredProperties;
            }

            return JsonSerializer.Serialize(schema, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JSON Schema for type {TypeName}", type.Name);
            throw;
        }
    }

    /// <summary>
    /// Generates a JSON Schema from a DataTable
    /// </summary>
    public string GenerateFromDataTable(DataTable dataTable, string name)
    {
        try
        {
            _logger.LogDebug("Generating JSON Schema for DataTable {Name}", name);
            
            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["title"] = name,
                ["properties"] = new Dictionary<string, object>()
            };

            foreach (DataColumn column in dataTable.Columns)
            {
                var columnSchema = new Dictionary<string, object>
                {
                    ["type"] = GetJsonSchemaType(column.DataType)
                };

                if (column.AllowDBNull)
                {
                    columnSchema["nullable"] = true;
                }

                // Add column metadata
                var metadata = new Dictionary<string, object>
                {
                    ["allowDBNull"] = column.AllowDBNull,
                    ["dataType"] = column.DataType.Name
                };

                if (column.AutoIncrement)
                {
                    metadata["autoIncrement"] = true;
                    metadata["autoIncrementSeed"] = column.AutoIncrementSeed;
                    metadata["autoIncrementStep"] = column.AutoIncrementStep;
                }

                if (column.Unique)
                {
                    metadata["isUnique"] = true;
                }

                if (!string.IsNullOrEmpty(column.Expression))
                {
                    metadata["expression"] = column.Expression;
                }

                columnSchema["x-column-metadata"] = metadata;

                ((Dictionary<string, object>)schema["properties"])[column.ColumnName] = columnSchema;
            }

            return JsonSerializer.Serialize(schema, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JSON Schema for DataTable {Name}", name);
            throw;
        }
    }

    private Dictionary<string, object> GetPropertySchema(PropertyInfo propertyInfo, Type containerType)
    {
        var schema = new Dictionary<string, object>();
        var type = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

        var typeInfo = GetJsonSchemaType(type);
        if (typeInfo is Dictionary<string, object> typeDict)
        {
            foreach (var kvp in typeDict)
            {
                schema[kvp.Key] = kvp.Value;
            }
        }
        else
        {
            schema["type"] = typeInfo;
        }
        
        if (Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null)
        {
            schema["nullable"] = true;
        }

        // Add Entity Framework specific metadata
        var metadata = new Dictionary<string, object>();

        if (_efModel != null)
        {
            if (CustomAttributeExtensions.GetCustomAttribute<DtoForAttribute>(containerType) != null)
            {
                string typeString = CustomAttributeExtensions.GetCustomAttribute<DtoForAttribute>(containerType).EntityType;

                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (a.GetTypes().Any(t => t.FullName == typeString))
                        containerType = a.GetTypes().First(t => t.FullName == typeString);
                }
                
                if (containerType == null)
                {
                    _logger.LogError("Type {TypeString} not found in EF model", typeString);
                    throw new Exception($"Type {typeString} not found in EF model");
                }
            }
            var efEntityType = _efModel.FindEntityType(containerType);
            if (efEntityType != null)
            {
                var efProperty = efEntityType.FindProperty(propertyInfo.Name);
                if (efProperty != null)
                {
                    // Primary Key
                    if (efProperty.IsPrimaryKey())
                    {
                        metadata["isPrimaryKey"] = true;
                        if (efProperty.ValueGenerated == ValueGenerated.OnAdd)
                        {
                            metadata["isIdentity"] = true;
                        }
                    }

                    // Foreign Keys
                    var foreignKeys = efProperty.GetContainingForeignKeys();
                    if (foreignKeys.Any())
                    {
                        var fk = foreignKeys.First();
                        metadata["isForeignKey"] = true;
                        metadata["foreignKeyTable"] = fk.PrincipalEntityType.GetTableName();
                        metadata["foreignKeyColumn"] = fk.PrincipalKey.Properties.First().Name;
                        metadata["foreignKeyConstraintName"] = fk.GetConstraintName();
                    }

                    // Column metadata
                    metadata["columnName"] = efProperty.GetColumnName();
                    metadata["columnType"] = efProperty.GetColumnType();
                    
                    var maxLength = efProperty.GetMaxLength();
                    if (maxLength.HasValue)
                    {
                        metadata["maxLength"] = maxLength.Value;
                        schema["maxLength"] = maxLength.Value;
                    }

                    // Default value
                    var defaultValue = efProperty.GetDefaultValue();
                    if (defaultValue != null)
                    {
                        metadata["defaultValue"] = defaultValue;
                    }
                    var defaultValueSql = efProperty.GetDefaultValueSql();
                    if (!string.IsNullOrEmpty(defaultValueSql))
                    {
                        metadata["defaultValueSql"] = defaultValueSql;
                    }

                    // Computed column
                    var computedColumnSql = efProperty.GetComputedColumnSql();
                    if (!string.IsNullOrEmpty(computedColumnSql))
                    {
                        metadata["computedColumnSql"] = computedColumnSql;
                    }

                    metadata["isRequired"] = !efProperty.IsNullable;
                }

                // Navigation Properties
                var navigation = efEntityType.FindNavigation(propertyInfo.Name);
                if (navigation != null)
                {
                    if (navigation.IsCollection)
                    {
                        metadata["isCollection"] = true;
                        metadata["elementType"] = navigation.TargetEntityType.Name;
                        metadata["foreignKeyProperty"] = navigation.ForeignKey.Properties.First().Name;
                    }
                    else
                    {
                        metadata["isNavigation"] = true;
                        metadata["navigationType"] = navigation.TargetEntityType.Name;
                        metadata["foreignKeyProperty"] = navigation.ForeignKey.Properties.First().Name;
                    }
                }
            }
        }
        else
        {
            // Fallback to attribute-based metadata when EF Core model is not available
            // Primary Key
            if (CustomAttributeExtensions.GetCustomAttribute<KeyAttribute>(propertyInfo) != null)
            {
                metadata["isPrimaryKey"] = true;
            }

            // Foreign Key
            var fkAttribute = CustomAttributeExtensions.GetCustomAttribute<ForeignKeyAttribute>(propertyInfo);
            if (fkAttribute != null)
            {
                metadata["isForeignKey"] = true;
                metadata["foreignKeyProperty"] = fkAttribute.Name;
            }

            // Navigation Property
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                var elementType = type.GetGenericArguments().FirstOrDefault();
                if (elementType != null)
                {
                    metadata["isCollection"] = true;
                    metadata["elementType"] = elementType.Name;
                }
            }
            else if (!type.IsPrimitive && type != typeof(string) && type != typeof(DateTime))
            {
                metadata["isNavigation"] = true;
                metadata["navigationType"] = type.Name;
            }

            // Column metadata
            var columnAttr = CustomAttributeExtensions.GetCustomAttribute<ColumnAttribute>(propertyInfo);
            if (columnAttr != null)
            {
                if (!string.IsNullOrEmpty(columnAttr.TypeName))
                    metadata["columnType"] = columnAttr.TypeName;
                if (columnAttr.Order > 0)
                    metadata["columnOrder"] = columnAttr.Order;
            }

            // Max Length
            var stringLengthAttr = CustomAttributeExtensions.GetCustomAttribute<StringLengthAttribute>(propertyInfo);
            if (stringLengthAttr != null)
            {
                schema["maxLength"] = stringLengthAttr.MaximumLength;
                metadata["maxLength"] = stringLengthAttr.MaximumLength;
                if (stringLengthAttr.MinimumLength > 0)
                {
                    schema["minLength"] = stringLengthAttr.MinimumLength;
                    metadata["minLength"] = stringLengthAttr.MinimumLength;
                }
            }
        }

        // Add validation attributes
        var rangeAttr = CustomAttributeExtensions.GetCustomAttribute<RangeAttribute>(propertyInfo);
        if (rangeAttr != null)
        {
            schema["minimum"] = rangeAttr.Minimum;
            schema["maximum"] = rangeAttr.Maximum;
        }

        var regexAttr = CustomAttributeExtensions.GetCustomAttribute<RegularExpressionAttribute>(propertyInfo);
        if (regexAttr != null)
        {
            schema["pattern"] = regexAttr.Pattern;
        }

        // Add metadata if any was collected
        if (metadata.Any())
        {
            schema["x-entity-metadata"] = metadata;
        }

        return schema;
    }

    private object GetJsonSchemaType(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            return "integer";
        if (type == typeof(double) || type == typeof(decimal) || type == typeof(float))
            return "number";
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(DateTime))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["format"] = "date-time"
            };
        }
        if (type == typeof(TimeOnly))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["format"] = "time"
            };
        }
        if (type == typeof(DateOnly))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["format"] = "date"
            };
        }
        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            return "array";
        return "object";
    }
} 