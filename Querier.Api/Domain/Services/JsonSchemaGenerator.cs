using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Querier.Api.Domain.Common.Models;

namespace Querier.Api.Domain.Services
{
    public class JsonSchemaGenerator
    {
        private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = false };

        public string GenerateSchema(Type type)
        {
            if (type == null) return null;

            if (type.IsGenericType)
            {
                var schema = HandleGenericType(type);
                if (schema != null)
                    return schema;
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

            return JsonSerializer.Serialize(baseSchema, _serializerOptions);
        }

        private string HandleGenericType(Type type)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            var genericArgs = type.GetGenericArguments();

            if (genericTypeDef == typeof(PagedResult<>))
                return HandlePagedResult(genericArgs[0]);

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return HandleEnumerable(genericArgs[0]);

            if (genericTypeDef == typeof(Task<>))
                return GenerateSchema(genericArgs[0]);

            if (genericTypeDef == typeof(Nullable<>))
                return HandleNullable(genericArgs[0]);

            return null;
        }

        private string HandlePagedResult(Type itemType)
        {
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

        private string HandleEnumerable(Type itemType)
        {
            var schema = new
            {
                type = "array",
                description = $"List of {itemType.Name}",
                items = JsonSerializer.Deserialize<object>(GenerateSchema(itemType))
            };
            return JsonSerializer.Serialize(schema, _serializerOptions);
        }

        private string HandleNullable(Type underlyingType)
        {
            var schema = JsonSerializer.Deserialize<object>(GenerateSchema(underlyingType));
            var schemaDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(schema));
            schemaDict["nullable"] = true;
            return JsonSerializer.Serialize(schemaDict, _serializerOptions);
        }

        private string[] GetRequiredProperties(Type type)
        {
            if (!type.IsClass || type == typeof(string)) return null;

            return type.GetProperties()
                .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null)
                .Select(p => p.Name)
                .ToArray();
        }

        private object GetMinValue(Type type) => type.GetCustomAttribute<RangeAttribute>()?.Minimum;
        private object GetMaxValue(Type type) => type.GetCustomAttribute<RangeAttribute>()?.Maximum;
        private int? GetMinLength(Type type) => type.GetCustomAttribute<StringLengthAttribute>()?.MinimumLength;
        private int? GetMaxLength(Type type) => type.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;
        private string GetPattern(Type type) => type.GetCustomAttribute<RegularExpressionAttribute>()?.Pattern;

        private string GetJsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long)) return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) return "number";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(DateTime)) return "string";
            if (type.IsArray || typeof(IEnumerable<>).IsAssignableFrom(type)) return "array";
            if (type.IsEnum) return "string";
            if (type.IsClass && type != typeof(string)) return "object";
            return "string";
        }

        private string GetJsonFormat(Type type)
        {
            if (type == typeof(DateTime)) return "date-time";
            if (type == typeof(int)) return "int32";
            if (type == typeof(long)) return "int64";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(string))
            {
                if (type.GetCustomAttribute<EmailAddressAttribute>() != null) return "email";
                if (type.GetCustomAttribute<PhoneAttribute>() != null) return "phone";
                if (type.GetCustomAttribute<UrlAttribute>() != null) return "uri";
            }
            return null;
        }

        private object GetJsonProperties(Type type)
        {
            if (!type.IsClass || type == typeof(string)) return null;

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

            return properties.Count > 0 ? properties : null;
        }
    }
} 