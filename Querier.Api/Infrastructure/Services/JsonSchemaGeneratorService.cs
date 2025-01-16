using System;
using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Querier.Api.Infrastructure.Services;

/// <summary>
/// Service for generating JSON Schema from various sources
/// </summary>
public class JsonSchemaGeneratorService
{
    private readonly ILogger _logger;
    private IModel _efModel;

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
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null &&
                           p.GetCustomAttribute<JsonIgnoreAttribute>() == null);

            var requiredProperties = new List<string>();

            foreach (var prop in properties)
            {
                var propSchema = GetPropertySchema(prop, type);
                ((Dictionary<string, object>)schema["properties"])[prop.Name] = propSchema;

                if (prop.GetCustomAttribute<RequiredAttribute>() != null)
                {
                    requiredProperties.Add(prop.Name);
                }
            }

            if (requiredProperties.Any())
            {
                schema["required"] = requiredProperties;
            }

            return JsonSerializer.Serialize(schema);
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
                    ["type"] = GetJsonSchemaType(column.DataType),
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

            return JsonSerializer.Serialize(schema);
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

        schema["type"] = GetJsonSchemaType(type);
        
        if (Nullable.GetUnderlyingType(propertyInfo.PropertyType) != null)
        {
            schema["nullable"] = true;
        }

        // Add Entity Framework specific metadata
        var metadata = new Dictionary<string, object>();

        if (_efModel != null)
        {
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
            if (propertyInfo.GetCustomAttribute<KeyAttribute>() != null)
            {
                metadata["isPrimaryKey"] = true;
            }

            // Foreign Key
            var fkAttribute = propertyInfo.GetCustomAttribute<ForeignKeyAttribute>();
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
            var columnAttr = propertyInfo.GetCustomAttribute<ColumnAttribute>();
            if (columnAttr != null)
            {
                if (!string.IsNullOrEmpty(columnAttr.TypeName))
                    metadata["columnType"] = columnAttr.TypeName;
                if (columnAttr.Order > 0)
                    metadata["columnOrder"] = columnAttr.Order;
            }

            // Max Length
            var stringLengthAttr = propertyInfo.GetCustomAttribute<StringLengthAttribute>();
            if (stringLengthAttr != null)
            {
                schema["maxLength"] = stringLengthAttr.MaximumLength;
                if (stringLengthAttr.MinimumLength > 0)
                    schema["minLength"] = stringLengthAttr.MinimumLength;
            }
        }

        // Add validation attributes
        var rangeAttr = propertyInfo.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr != null)
        {
            schema["minimum"] = rangeAttr.Minimum;
            schema["maximum"] = rangeAttr.Maximum;
        }

        var regexAttr = propertyInfo.GetCustomAttribute<RegularExpressionAttribute>();
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

    private string GetJsonSchemaType(Type type)
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
            return "string"; // with format: date-time
        if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            return "array";
        return "object";
    }
} 