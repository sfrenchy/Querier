using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Models;

namespace Querier.Api.Domain.Services
{
    public class JsonSchemaGenerator
    {
        private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = false };
        private readonly ILogger<JsonSchemaGenerator> _logger;

        public JsonSchemaGenerator(ILogger<JsonSchemaGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GenerateSchema(Type type)
        {
            try
            {
                if (type == null)
                {
                    _logger.LogWarning("Attempted to generate schema for null type");
                    return null;
                }

                _logger.LogDebug("Generating JSON schema for type: {TypeName}", type.FullName);

                if (type.IsGenericType)
                {
                    _logger.LogTrace("Processing generic type: {TypeName}", type.FullName);
                    var schema = HandleGenericType(type);
                    if (schema != null)
                    {
                        _logger.LogDebug("Generated schema for generic type: {TypeName}", type.FullName);
                        return schema;
                    }
                }

                var baseSchema = new
                {
                    type = GetJsonType(type),
                    format = GetJsonFormat(type),
                    description = type.GetCustomAttribute<SummaryAttribute>()?.Summary,
                    required = GetRequiredProperties(type),
                    properties = GetJsonProperties(type),
                    @enum = type.IsEnum ? Enum.GetNames(type) : null,
                    minimum = GetMinValue(type),
                    maximum = GetMaxValue(type),
                    minLength = GetMinLength(type),
                    maxLength = GetMaxLength(type),
                    pattern = GetPattern(type)
                };

                var result = JsonSerializer.Serialize(baseSchema, _serializerOptions);
                _logger.LogDebug("Successfully generated schema for type: {TypeName}", type.FullName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating schema for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private string HandleGenericType(Type type)
        {
            try
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();

                _logger.LogTrace("Handling generic type: {TypeName} with {ArgCount} arguments", 
                    type.FullName, genericArgs.Length);

                if (genericTypeDef == typeof(PagedResult<>))
                {
                    _logger.LogTrace("Processing PagedResult<T> for type: {TypeName}", genericArgs[0].FullName);
                    return HandlePagedResult(genericArgs[0]);
                }

                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    _logger.LogTrace("Processing IEnumerable<T> for type: {TypeName}", genericArgs[0].FullName);
                    return HandleEnumerable(genericArgs[0]);
                }

                if (genericTypeDef == typeof(Task<>))
                {
                    _logger.LogTrace("Processing Task<T> for type: {TypeName}", genericArgs[0].FullName);
                    return GenerateSchema(genericArgs[0]);
                }

                if (genericTypeDef == typeof(Nullable<>))
                {
                    _logger.LogTrace("Processing Nullable<T> for type: {TypeName}", genericArgs[0].FullName);
                    return HandleNullable(genericArgs[0]);
                }

                _logger.LogTrace("No specific handler for generic type: {TypeName}", type.FullName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling generic type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private string HandlePagedResult(Type itemType)
        {
            try
            {
                _logger.LogTrace("Creating schema for PagedResult<{TypeName}>", itemType.FullName);
                var schema = new
                {
                    type = "object",
                    description = "Paginated result list",
                    properties = new
                    {
                        items = new
                        {
                            type = "array",
                            description = "List of items",
                            items = JsonSerializer.Deserialize<object>(GenerateSchema(itemType))
                        },
                        total = new
                        {
                            type = "integer",
                            format = "int32",
                            description = "Total number of items"
                        }
                    }
                };
                return JsonSerializer.Serialize(schema, _serializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling paged result for type: {TypeName}", itemType?.FullName);
                throw;
            }
        }

        private string HandleEnumerable(Type itemType)
        {
            try
            {
                _logger.LogTrace("Creating schema for IEnumerable<{TypeName}>", itemType.FullName);
                var schema = new
                {
                    type = "array",
                    description = $"List of {itemType.Name}",
                    items = JsonSerializer.Deserialize<object>(GenerateSchema(itemType))
                };
                return JsonSerializer.Serialize(schema, _serializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling enumerable for type: {TypeName}", itemType?.FullName);
                throw;
            }
        }

        private string HandleNullable(Type underlyingType)
        {
            try
            {
                _logger.LogTrace("Creating schema for Nullable<{TypeName}>", underlyingType.FullName);
                var schema = JsonSerializer.Deserialize<object>(GenerateSchema(underlyingType));
                var schemaDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(schema));
                schemaDict["nullable"] = true;
                return JsonSerializer.Serialize(schemaDict, _serializerOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling nullable for type: {TypeName}", underlyingType?.FullName);
                throw;
            }
        }

        private string[] GetRequiredProperties(Type type)
        {
            try
            {
                if (!type.IsClass || type == typeof(string))
                {
                    _logger.LogTrace("No required properties for type: {TypeName}", type.FullName);
                    return null;
                }

                var required = type.GetProperties()
                    .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null)
                    .Select(p => p.Name)
                    .ToArray();

                _logger.LogTrace("Found {Count} required properties for type: {TypeName}", 
                    required.Length, type.FullName);
                return required;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting required properties for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private object GetMinValue(Type type)
        {
            try
            {
                var attr = type.GetCustomAttribute<RangeAttribute>();
                if (attr != null)
                {
                    _logger.LogTrace("Found minimum value {Value} for type: {TypeName}", 
                        attr.Minimum, type.FullName);
                }
                return attr?.Minimum;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting minimum value for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private object GetMaxValue(Type type)
        {
            try
            {
                var attr = type.GetCustomAttribute<RangeAttribute>();
                if (attr != null)
                {
                    _logger.LogTrace("Found maximum value {Value} for type: {TypeName}", 
                        attr.Maximum, type.FullName);
                }
                return attr?.Maximum;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting maximum value for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private int? GetMinLength(Type type)
        {
            try
            {
                var attr = type.GetCustomAttribute<StringLengthAttribute>();
                if (attr != null)
                {
                    _logger.LogTrace("Found minimum length {Length} for type: {TypeName}", 
                        attr.MinimumLength, type.FullName);
                }
                return attr?.MinimumLength;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting minimum length for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private int? GetMaxLength(Type type)
        {
            try
            {
                var attr = type.GetCustomAttribute<StringLengthAttribute>();
                if (attr != null)
                {
                    _logger.LogTrace("Found maximum length {Length} for type: {TypeName}", 
                        attr.MaximumLength, type.FullName);
                }
                return attr?.MaximumLength;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting maximum length for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private string GetPattern(Type type)
        {
            try
            {
                var attr = type.GetCustomAttribute<RegularExpressionAttribute>();
                if (attr != null)
                {
                    _logger.LogTrace("Found pattern {Pattern} for type: {TypeName}", 
                        attr.Pattern, type.FullName);
                }
                return attr?.Pattern;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pattern for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private string GetJsonType(Type type)
        {
            try
            {
                string jsonType;
                if (type == typeof(string)) jsonType = "string";
                else if (type == typeof(int) || type == typeof(long)) jsonType = "integer";
                else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) jsonType = "number";
                else if (type == typeof(bool)) jsonType = "boolean";
                else if (type == typeof(DateTime)) jsonType = "string";
                else if (type.IsArray || typeof(IEnumerable<>).IsAssignableFrom(type)) jsonType = "array";
                else if (type.IsEnum) jsonType = "string";
                else if (type.IsClass && type != typeof(string)) jsonType = "object";
                else jsonType = "string";

                _logger.LogTrace("Mapped type {TypeName} to JSON type: {JsonType}", type.FullName, jsonType);
                return jsonType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting JSON type for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private string GetJsonFormat(Type type)
        {
            try
            {
                string format = null;
                if (type == typeof(DateTime)) format = "date-time";
                else if (type == typeof(int)) format = "int32";
                else if (type == typeof(long)) format = "int64";
                else if (type == typeof(float)) format = "float";
                else if (type == typeof(double)) format = "double";
                else if (type == typeof(decimal)) format = "decimal";
                else if (type == typeof(string))
                {
                    if (type.GetCustomAttribute<EmailAddressAttribute>() != null) format = "email";
                    else if (type.GetCustomAttribute<PhoneAttribute>() != null) format = "phone";
                    else if (type.GetCustomAttribute<UrlAttribute>() != null) format = "uri";
                }

                if (format != null)
                {
                    _logger.LogTrace("Mapped type {TypeName} to JSON format: {Format}", type.FullName, format);
                }
                return format;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting JSON format for type: {TypeName}", type?.FullName);
                throw;
            }
        }

        private object GetJsonProperties(Type type)
        {
            try
            {
                if (!type.IsClass || type == typeof(string))
                {
                    _logger.LogTrace("No properties to map for type: {TypeName}", type.FullName);
                    return null;
                }

                _logger.LogTrace("Mapping properties for type: {TypeName}", type.FullName);
                var properties = type.GetProperties()
                    .Where(p => p.CanRead && p.CanWrite)
                    .ToDictionary(
                        p => p.Name,
                        p => new
                        {
                            type = GetJsonType(p.PropertyType),
                            format = GetJsonFormat(p.PropertyType),
                            description = p.GetCustomAttribute<SummaryAttribute>()?.Summary,
                            required = p.GetCustomAttribute<RequiredAttribute>() != null,
                            @enum = p.PropertyType.IsEnum ? Enum.GetNames(p.PropertyType) : null,
                            minimum = GetMinValue(p.PropertyType),
                            maximum = GetMaxValue(p.PropertyType),
                            minLength = GetMinLength(p.PropertyType),
                            maxLength = GetMaxLength(p.PropertyType),
                            pattern = GetPattern(p.PropertyType)
                        }
                    );

                _logger.LogTrace("Mapped {Count} properties for type: {TypeName}", 
                    properties.Count, type.FullName);
                return properties.Count > 0 ? properties : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting JSON properties for type: {TypeName}", type?.FullName);
                throw;
            }
        }
    }
} 