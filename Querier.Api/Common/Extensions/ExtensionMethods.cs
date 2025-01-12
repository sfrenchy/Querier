using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Common.Attributes;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Tools;

namespace Querier.Api.Common.Extensions
{
    public static class ExtensionMethods
    {
        private static readonly ILogger LOGGER;

        static ExtensionMethods()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
            LOGGER = loggerFactory.CreateLogger("ExtensionMethods");
        }

        private static readonly byte[] BMP = [66, 77];
        private static readonly byte[] DOC = [208, 207, 17, 224, 161, 177, 26, 225];
        private static readonly byte[] EXE_DLL = [77, 90];
        private static readonly byte[] GIF = [71, 73, 70, 56];
        private static readonly byte[] ICO = [0, 0, 1, 0];
        private static readonly byte[] JPG = [255, 216, 255];
        private static readonly byte[] MP3 = [255, 251, 48];
        private static readonly byte[] OGG = [79, 103, 103, 83, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0];
        private static readonly byte[] PDF = [37, 80, 68, 70, 45, 49, 46];
        private static readonly byte[] PNG = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82];
        private static readonly byte[] RAR = [82, 97, 114, 33, 26, 7, 0];
        private static readonly byte[] SWF = [70, 87, 83];
        private static readonly byte[] TIFF = [73, 73, 42, 0];
        private static readonly byte[] TORRENT = [100, 56, 58, 97, 110, 110, 111, 117, 110, 99, 101];
        private static readonly byte[] TTF = [0, 1, 0, 0, 0];
        private static readonly byte[] WAV_AVI = [82, 73, 70, 70];
        private static readonly byte[] WMV_WMA = [48, 38, 178, 117, 142, 102, 207, 17, 166, 217, 0, 170, 0, 98, 206, 108
        ];
        private static readonly byte[] ZIP_DOCX = [80, 75, 3, 4];

        private static readonly MethodInfo SET_METHOD =
            typeof(DbContext).GetMethod(nameof(DbContext.Set), 1, []) ??
            throw new Exception("Type not found: DbContext.Set");

        public static IQueryable Query(this DbContext context, string entityName)
        {
            try
            {
                LOGGER.LogDebug("Querying entity by name: {EntityName}", entityName);
                var entityType = context.Model.FindEntityType(entityName)?.ClrType;
                if (entityType == null)
                {
                    LOGGER.LogWarning("Entity type not found: {EntityName}", entityName);
                    throw new ArgumentException($"Entity type not found: {entityName}");
                }
                return context.Query(entityType);
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error querying entity by name: {EntityName}", entityName);
                throw;
            }
        }

        public static IQueryable Query(this DbContext context, Type entityType)
        {
            try
            {
                LOGGER.LogDebug("Querying entity by type: {EntityType}", entityType.FullName);
                var result = (IQueryable)SET_METHOD.MakeGenericMethod(entityType).Invoke(context, null);
                if (result == null)
                {
                    LOGGER.LogWarning("Failed to create query for type: {EntityType}", entityType.FullName);
                    throw new InvalidOperationException($"Failed to create query for type: {entityType.FullName}");
                }
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error querying entity by type: {EntityType}", entityType.FullName);
                throw;
            }
        }

        public static DataTable ToDataTable<T>(this IList<T> data)
        {
            try
            {
                LOGGER.LogDebug("Converting list to DataTable, type: {Type}, count: {Count}", typeof(T).Name, data.Count);
                var table = new DataTable();

            if (data.Count > 0)
            {
                    var properties = TypeDescriptor.GetProperties(data[0].GetType());
                foreach (PropertyDescriptor prop in properties)
                    {
                        var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        table.Columns.Add(prop.Name, type);
                    }

                foreach (T item in data)
                {
                        var row = table.NewRow();
                    foreach (PropertyDescriptor prop in properties)
                        {
                        row[prop.Name] = prop.GetValue(item) ?? DBNull.Value;
                        }
                    table.Rows.Add(row);
                }
            }
            
                LOGGER.LogInformation("Successfully converted list to DataTable with {ColumnCount} columns and {RowCount} rows", 
                    table.Columns.Count, table.Rows.Count);
            return table;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error converting list to DataTable, type: {Type}", typeof(T).Name);
                throw;
            }
        }

        public static object ExecuteScalar(this DbContext context, string sql,
        List<DbParameter> parameters = null,
        CommandType commandType = CommandType.Text,
        int? commandTimeOutInSeconds = null)
        {
            try
            {
                LOGGER.LogDebug("Executing scalar SQL: {Sql}", sql);
                var value = ExecuteScalar(context.Database, sql, parameters, commandType, commandTimeOutInSeconds);
                LOGGER.LogInformation("Successfully executed scalar SQL");
            return value;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error executing scalar SQL: {Sql}", sql);
                throw;
            }
        }

        public static DataTable RawSqlQuery(this DatabaseFacade database, string sql, 
            List<DbParameter> parameters = null, 
            CommandType commandType = CommandType.Text, 
            int? commandTimeOutInSeconds = null)
        {
            try
            {
                LOGGER.LogDebug("Executing raw SQL query: {Sql}", sql);
            var dt = new DataTable();
            using (var command = database.GetDbConnection().CreateCommand())
            {
                    command.Connection!.Open();
                command.CommandText = sql;
                    command.CommandType = commandType;

                    if (commandTimeOutInSeconds.HasValue)
                {
                        command.CommandTimeout = commandTimeOutInSeconds.Value;
                    }

                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        LOGGER.LogDebug("Added {Count} parameters to SQL query", parameters.Count);
                    }
                    
                    using (var reader = command.ExecuteReader())
                    {
                    dt.Load(reader);
                }
            }

                LOGGER.LogInformation("Successfully executed raw SQL query. Returned {RowCount} rows", dt.Rows.Count);
            return dt;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error executing raw SQL query: {Sql}", sql);
                throw;
            }
        }

        public static object ExecuteScalar(this DatabaseFacade database,
        string sql, List<DbParameter> parameters = null,
        CommandType commandType = CommandType.Text,
        int? commandTimeOutInSeconds = null)
        {
            Object value;
            using (var cmd = database.GetDbConnection().CreateCommand())
            {
                if (cmd.Connection!.State != ConnectionState.Open)
                {
                    cmd.Connection.Open();
                }
                cmd.CommandText = sql;
                cmd.CommandType = commandType;
                if (commandTimeOutInSeconds != null)
                {
                    cmd.CommandTimeout = (int)commandTimeOutInSeconds;
                }
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                }
                value = cmd.ExecuteScalar();
            }
            return value;
        }

        public static DataTable ToDataTable(this IEnumerable<object> objects)
        {
            try
            {
                LOGGER.LogDebug("Converting object enumerable to DataTable");
                var table = new DataTable();

            if (objects.Any())
            {
                var properties = objects.First().GetType().GetProperties();
                    LOGGER.LogDebug("Adding {Count} columns based on object properties", properties.Length);
                    
                foreach (var property in properties)
                {
                        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                        table.Columns.Add(property.Name, type);
                }

                    LOGGER.LogDebug("Adding rows to DataTable");
                foreach (var obj in objects)
                {
                        var row = table.NewRow();
                    foreach (var property in properties)
                    {
                        row[property.Name] = property.GetValue(obj) ?? DBNull.Value;
                    }
                    table.Rows.Add(row);
                }
            }

                LOGGER.LogInformation("Successfully converted objects to DataTable with {ColumnCount} columns and {RowCount} rows",
                    table.Columns.Count, table.Rows.Count);
            return table;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error converting object enumerable to DataTable");
                throw;
            }
        }

        public static IServiceCollection AddLazyResolution(this IServiceCollection services)
        {
            try
            {
                LOGGER.LogDebug("Adding lazy resolution to service collection");
                var result = services.AddTransient(typeof(Lazy<>), typeof(LazilyResolved<>));
                LOGGER.LogInformation("Successfully added lazy resolution to service collection");
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error adding lazy resolution to service collection");
                throw;
            }
        }

        public static int MonthDifference(this DateTime lValue, DateTime rValue)
        {
            try
            {
                LOGGER.LogDebug("Calculating month difference between {Date1} and {Date2}", lValue, rValue);
                var result = Math.Abs((lValue.Month - rValue.Month) + 12 * (lValue.Year - rValue.Year));
                LOGGER.LogDebug("Month difference is {Months} months", result);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error calculating month difference between {Date1} and {Date2}", lValue, rValue);
                throw;
            }
        }

        public static DateTime FromTimeZone(this DateTimeOffset? lValue, string timeZone)
        {
            try
            {
                if (!lValue.HasValue)
                {
                    LOGGER.LogWarning("Null DateTimeOffset provided for timezone conversion");
                    throw new ArgumentNullException(nameof(lValue));
                }

                LOGGER.LogDebug("Converting DateTimeOffset {DateTime} to timezone {TimeZone}", lValue, timeZone);
                var tZone = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                var result = TimeZoneInfo.ConvertTimeFromUtc(lValue.Value.DateTime, tZone);
                LOGGER.LogDebug("Converted time is {DateTime}", result);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error converting DateTimeOffset {DateTime} to timezone {TimeZone}", lValue, timeZone);
                throw;
            }
        }

        public static bool Between(this DateTime input, DateTime date1, DateTime date2)
        {
            try
            {
                LOGGER.LogDebug("Checking if {Date} is between {StartDate} and {EndDate}", input, date1, date2);
                var result = (input > date1 && input < date2);
                LOGGER.LogDebug("Date check result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error checking if {Date} is between {StartDate} and {EndDate}", input, date1, date2);
                throw;
            }
        }

        public static T CastTo<T>(this object o) => (T)o;
        public static T CastTo<T>(this object o, T type) => (T)o;

        public static List<dynamic> CastListToDynamic<T>(this List<T> source)
        {
            try
            {
                LOGGER.LogDebug("Converting List<{Type}> to List<dynamic>, count: {Count}", typeof(T).Name, source.Count);
                var result = new List<dynamic>();
            foreach (var item in source)
            {
                result.Add(item);
            }
                LOGGER.LogInformation("Successfully converted {Count} items to dynamic list", result.Count);
            return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error converting List<{Type}> to List<dynamic>", typeof(T).Name);
                throw;
            }
        }

        public static List<dynamic> CastToDynamic(this IEnumerable source)
        {
            try
            {
                LOGGER.LogDebug("Converting IEnumerable to List<dynamic>");
                var result = new List<dynamic>();
            foreach (var item in source)
            {
                result.Add((dynamic)item);
            }
                LOGGER.LogInformation("Successfully converted {Count} items to dynamic list", result.Count);
            return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error converting IEnumerable to List<dynamic>");
                throw;
            }
        }

        public static bool IsNullableProperty(this PropertyInfo propertyInfo)
        {
            try
            {
                LOGGER.LogDebug("Checking if property {PropertyName} is nullable", propertyInfo.Name);
                var result = propertyInfo.PropertyType.Name.IndexOf("Nullable`", StringComparison.Ordinal) > -1;
                LOGGER.LogDebug("Property {PropertyName} nullable check result: {Result}", propertyInfo.Name, result);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error checking if property {PropertyName} is nullable", propertyInfo.Name);
                throw;
            }
        }

        public static async Task<object> InvokeAsync(this MethodInfo @this, object obj, params object[] parameters)
        {
            try
            {
                LOGGER.LogDebug("Invoking async method {MethodName} on type {TypeName}", @this.Name, obj.GetType().Name);
            var task = (Task)@this.Invoke(obj, parameters);
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
                var result = resultProperty.GetValue(task);
                LOGGER.LogInformation("Successfully invoked async method {MethodName}", @this.Name);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error invoking async method {MethodName} on type {TypeName}", @this.Name, obj.GetType().Name);
                throw;
            }
        }

        public static EntityDefinitionDto ToEntityDefinition(this Type type)
        {
            try
            {
                LOGGER.LogDebug("Converting type {TypeName} to EntityDefinition", type.Name);
                var result = new EntityDefinitionDto
                {
                    Name = type.FullName,
                    Properties = []
                };

                var properties = type.GetProperties()
                    .Where(p => p.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute)) == null &&
                               p.GetCustomAttribute(typeof(Newtonsoft.Json.JsonIgnoreAttribute)) == null);

                LOGGER.LogDebug("Processing {Count} properties for type {TypeName}", properties.Count(), type.Name);

                foreach (var pi in properties)
                {
                    var pd = new PropertyDefinitionDto
                    {
                        Name = pi.Name,
                        Type = pi.IsNullableProperty() ? Nullable.GetUnderlyingType(pi.PropertyType).Name + "?" : pi.PropertyType.Name,
                        Options = []
                    };
                
                if (pi.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.KeyAttribute)).Any())
                    {
                    pd.Options.Add(PropertyOption.IsKey);
                        LOGGER.LogDebug("Property {PropertyName} marked as Key", pi.Name);
                    }
                
                if (pi.IsNullableProperty())
                    {
                    pd.Options.Add(PropertyOption.IsNullable);
                        LOGGER.LogDebug("Property {PropertyName} marked as Nullable", pi.Name);
                    }

                if (pi.GetCustomAttributes(typeof(JsonStringAttribute)).Any())
                    {
                    pd.Type = "JsonString";
                        LOGGER.LogDebug("Property {PropertyName} marked as JsonString", pi.Name);
                    }

                result.Properties.Add(pd);
            }

                LOGGER.LogInformation("Successfully converted type {TypeName} to EntityDefinition with {PropertyCount} properties",
                    type.Name, result.Properties.Count);
            return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error converting type {TypeName} to EntityDefinition", type.Name);
                throw;
            }
        }

        public static bool IsNullOrEmpty(this JToken token)
        {
            try
            {
                LOGGER.LogDebug("Checking if JToken is null or empty");
                var result = token == null ||
                   token.Type == JTokenType.Array && !token.HasValues ||
                   token.Type == JTokenType.Object && !token.HasValues ||
                   token.Type == JTokenType.String && token.ToString() == string.Empty ||
                   token.Type == JTokenType.Null;
                LOGGER.LogDebug("JToken null or empty check result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error checking if JToken is null or empty");
                throw;
            }
        }

        public static DataTable Filter(this DataTable source, List<EntityCRUDDataFilterDto> filters)
        {
            try
            {
                LOGGER.LogDebug("Filtering DataTable with {Count} filters", filters?.Count ?? 0);
                
                if (source == null)
                {
                    LOGGER.LogWarning("Source DataTable is null");
                    throw new ArgumentNullException(nameof(source));
                }

                if (filters == null || filters.Count == 0)
                {
                    LOGGER.LogInformation("No filters provided, returning original DataTable");
                    return source;
                }

            var predicate = PredicateBuilder.True<DataRow>();
                foreach (var filter in filters)
            {
                string columnName = filter.Column.Name;
                if (source.Columns.Contains(columnName))
                {
                        LOGGER.LogDebug("Processing filter for column {ColumnName} with operator {Operator}", columnName, filter.Operator);
                        
                    int columnIndex = source.Columns.IndexOf(columnName);
                    DataColumn column = source.Columns[columnIndex];
                    Type columnType = column.DataType;
                        
                        try
                        {
                    object value = columnType.GetValueFromString(filter.Operand);
                            LOGGER.LogDebug("Successfully converted filter value for column {ColumnName}", columnName);

                    switch (columnType)
                    {
                        case Type when columnType == typeof(Int64):
                            Int64 int64Value = (Int64) value;
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (Int64)p[columnName] == int64Value);
                                    break;
                                case "NotEqualTo":
                                    predicate = predicate.And(p => (Int64)p[columnName] != int64Value);
                                    break;
                                case "GreaterThan":
                                    predicate = predicate.And(p => (Int64)p[columnName] > int64Value);
                                    break;
                                case "LessThan":
                                    predicate = predicate.And(p => (Int64)p[columnName] < int64Value);
                                    break;
                                case "EqualOrGreaterThan":
                                    predicate = predicate.And(p => (Int64)p[columnName] >= int64Value);
                                    break;
                                case "EqualOrLessThan":
                                    predicate = predicate.And(p => (Int64)p[columnName] <= int64Value);
                                    break;
                            }
                            break;
                         case Type when columnType == typeof(int):
                            int intValue = (int) value;
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (int)p[columnName] == intValue);
                                    break;
                                case "NotEqualTo":
                                    predicate = predicate.And(p => (int)p[columnName] != intValue);
                                    break;
                                case "GreaterThan":
                                    predicate = predicate.And(p => (int)p[columnName] > intValue);
                                    break;
                                case "LessThan":
                                    predicate = predicate.And(p => (int)p[columnName] < intValue);
                                    break;
                                case "EqualOrGreaterThan":
                                    predicate = predicate.And(p => (int)p[columnName] >= intValue);
                                    break;
                                case "EqualOrLessThan":
                                    predicate = predicate.And(p => (int)p[columnName] <= intValue);
                                    break;
                            }
                            break;
                        case Type when columnType == typeof(decimal):
                            decimal decimalValue = (decimal)value;
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (decimal)p[columnName] == decimalValue);
                                    break;
                                case "NotEqualTo":
                                    predicate = predicate.And(p => (decimal)p[columnName] != decimalValue);
                                    break;
                                case "GreaterThan":
                                    predicate = predicate.And(p => (decimal)p[columnName] > decimalValue);
                                    break;
                                case "LessThan":
                                    predicate = predicate.And(p => (decimal)p[columnName] < decimalValue);
                                    break;
                                case "EqualOrGreaterThan":
                                    predicate = predicate.And(p => (decimal)p[columnName] >= decimalValue);
                                    break;
                                case "EqualOrLessThan":
                                    predicate = predicate.And(p => (decimal)p[columnName] <= decimalValue);
                                    break;
                            }
                            break;
                        case Type when columnType == typeof(DateTime):
                            DateTime dateTimeValue = (DateTime) value; 
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (DateTime)p[columnName] == dateTimeValue);
                                    break;
                                case "NotEqualTo":
                                    predicate = predicate.And(p => (DateTime)p[columnName] != dateTimeValue);
                                    break;
                                case "GreaterThan":
                                    predicate = predicate.And(p => (DateTime)p[columnName] > dateTimeValue);
                                    break;
                                case "LessThan":
                                    predicate = predicate.And(p => (DateTime)p[columnName] < dateTimeValue);
                                    break;
                                case "EqualOrGreaterThan":
                                    predicate = predicate.And(p => (DateTime)p[columnName] >= dateTimeValue);
                                    break;
                                case "EqualOrLessThan":
                                    predicate = predicate.And(p => (DateTime)p[columnName] <= dateTimeValue);
                                    break;
                            }
                            break;
                        case Type when columnType == typeof(string):
                            string stringValue = (string)value;
                            switch (filter.Operator)
                            {
                                case "EqualTo":
                                    predicate = predicate.And(p => (string)p[columnName] == stringValue);
                                    break;
                                case "Contains":
                                    predicate = predicate.And(p => ((string)p[columnName]).Contains(stringValue));
                                    break;
                                case "BeginWith":
                                    predicate = predicate.And(p => ((string)p[columnName]).StartsWith(stringValue));
                                    break;
                                case "EndWith":
                                    predicate = predicate.And(p => ((string)p[columnName]).EndsWith(stringValue));
                                    break;
                                default:
                                    throw new NotImplementedException(
                                        $"Operator {filter.Operator} is not implemented for type {columnType}");
                            }
                            break;
                        default:
                            throw new NotImplementedException($"Type {columnType} filtering is not yet implemented");
                    }
                        }
                        catch (Exception ex)
                        {
                            LOGGER.LogWarning(ex, "Failed to process filter for column {ColumnName}", columnName);
                            throw new ArgumentException($"Invalid filter value for column {columnName}", ex);
                        }
                    }
                    else
                    {
                        LOGGER.LogWarning("Column {ColumnName} not found in DataTable", columnName);
                    }
                }

                var result = source.AsEnumerable().AsQueryable().Where(predicate).CopyToDataTable();
                LOGGER.LogInformation("Successfully filtered DataTable. Original rows: {OriginalCount}, Filtered rows: {FilteredCount}", 
                    source.Rows.Count, result.Rows.Count);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error filtering DataTable");
                throw;
            }
        }

        public static object GetValueFromString<T>(this T type, string val) where T: Type
        {
            try
            {
                LOGGER.LogDebug("Converting string value '{Value}' to type {Type}", val, type.Name);

                if (string.IsNullOrWhiteSpace(val))
                {
                    LOGGER.LogWarning("Empty value provided for conversion to type {Type}", type.Name);
                    throw new ArgumentException("Value cannot be null or empty", nameof(val));
                }

                object result = type switch
                {
                    Type t when t == typeof(Decimal) => Convert.ToDecimal(val),
                    Type t when t == typeof(Int64) => Convert.ToInt64(val),
                    Type t when t == typeof(int) => Convert.ToInt32(val),
                    Type t when t == typeof(string) => Convert.ToString(val),
                    Type t when t == typeof(DateTime) => DateTime.ParseExact(val, "yyyy-MM-dd", CultureInfo.CurrentCulture),
                    _ => throw new NotImplementedException($"Type {type} not handled yet")
                };

                LOGGER.LogDebug("Successfully converted value to type {Type}", type.Name);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error converting string value '{Value}' to type {Type}", val, type.Name);
                throw;
            }
        }

        public static string GetSpecificClaim(this ClaimsIdentity claimsIdentity, string claimType)
        {
            try
            {
                LOGGER.LogDebug("Getting specific claim of type: {ClaimType}", claimType);

                if (claimsIdentity == null)
                {
                    LOGGER.LogWarning("ClaimsIdentity is null");
                    throw new ArgumentNullException(nameof(claimsIdentity));
                }

            var claim = claimsIdentity.Claims.FirstOrDefault(x => x.Type == claimType);
                if (claim == null)
                {
                    LOGGER.LogDebug("Claim of type {ClaimType} not found", claimType);
                    return string.Empty;
                }

                LOGGER.LogDebug("Successfully retrieved claim of type {ClaimType}", claimType);
                return claim.Value;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error getting specific claim of type: {ClaimType}", claimType);
                throw;
            }
        }

        public static string GetHash<T>(this object instance) where T : HashAlgorithm, new()
        {
            try
            {
                LOGGER.LogDebug("Computing hash using algorithm: {Algorithm}", typeof(T).Name);
                
                if (instance == null)
                {
                    LOGGER.LogWarning("Instance is null");
                    throw new ArgumentNullException(nameof(instance));
                }

            T cryptoServiceProvider = new T();
                var result = computeHash(instance, cryptoServiceProvider);
                LOGGER.LogDebug("Successfully computed hash using {Algorithm}", typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error computing hash using algorithm: {Algorithm}", typeof(T).Name);
                throw;
            }
        }

        public static string GetKeyedHash<T>(this object instance, byte[] key) where T : KeyedHashAlgorithm, new()
        {
            try
            {
                LOGGER.LogDebug("Computing keyed hash using algorithm: {Algorithm}", typeof(T).Name);
                
                if (instance == null)
                {
                    LOGGER.LogWarning("Instance is null");
                    throw new ArgumentNullException(nameof(instance));
                }

                if (key == null || key.Length == 0)
                {
                    LOGGER.LogWarning("Key is null or empty");
                    throw new ArgumentException("Key cannot be null or empty", nameof(key));
                }

                T cryptoServiceProvider = new T { Key = key };
                var result = computeHash(instance, cryptoServiceProvider);
                LOGGER.LogDebug("Successfully computed keyed hash using {Algorithm}", typeof(T).Name);
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error computing keyed hash using algorithm: {Algorithm}", typeof(T).Name);
                throw;
            }
        }

        private static string computeHash<T>(object instance, T cryptoServiceProvider) where T : HashAlgorithm, new()
        {
            try
            {
                LOGGER.LogDebug("Computing hash for object of type: {Type}", instance.GetType().Name);
                
                using var memoryStream = new MemoryStream();
                var serializer = new DataContractSerializer(instance.GetType());
                
                serializer.WriteObject(memoryStream, instance);
                var hash = cryptoServiceProvider.ComputeHash(memoryStream.ToArray());
                var result = Convert.ToBase64String(hash);
                
                LOGGER.LogDebug("Successfully computed hash");
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error computing hash for object of type: {Type}", instance.GetType().Name);
                throw;
            }
        }

        public static string GetMD5Hash(this object instance)
        {
            try
            {
                LOGGER.LogDebug("Computing MD5 hash for object of type: {Type}", instance?.GetType().Name);
                var result = instance.GetHash<MD5CryptoServiceProvider>();
                LOGGER.LogDebug("Successfully computed MD5 hash");
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error computing MD5 hash for object of type: {Type}", instance?.GetType().Name);
                throw;
            }
        }

        public static string GetSHA1Hash(this object instance)
        {
            try
            {
                LOGGER.LogDebug("Computing SHA1 hash for object of type: {Type}", instance?.GetType().Name);
                var result = instance.GetHash<SHA1CryptoServiceProvider>();
                LOGGER.LogDebug("Successfully computed SHA1 hash");
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error computing SHA1 hash for object of type: {Type}", instance?.GetType().Name);
                throw;
            }
        }

        //Retire les accents d'une string
        public static string RemoveDiacritics(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    LOGGER.LogDebug("Empty text provided for diacritics removal");
                    return text;
                }

                LOGGER.LogDebug("Removing diacritics from text of length {Length}", text.Length);
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                    if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

                var result = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
                LOGGER.LogInformation("Successfully removed diacritics from text");
                return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error removing diacritics from text");
                throw;
            }
        }

        /// <summary>
        ///This method reads the first bytes of a file,
        ///and then uses the MimeTypeHelper class to determine the MIME type of the file based on the byte array.
        ///This method is more secure than just checking the file extension, 
        ///as it can detect if a file has been renamed to bypass file type restrictions.
        /// </summary>
        public static string GetMimeType(byte[] file, string fileName)
        {
            try
            {
                LOGGER.LogDebug("Determining MIME type for file: {FileName}", fileName);

                if (file == null || file.Length == 0)
                {
                    LOGGER.LogWarning("Empty file content provided for MIME type detection");
                    return "application/octet-stream";
                }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                    LOGGER.LogWarning("Empty filename provided for MIME type detection");
                    return "application/octet-stream";
            }

            //Get the file extension
                string extension = Path.GetExtension(fileName)?.ToUpper() ?? string.Empty;
                LOGGER.LogDebug("File extension: {Extension}", extension);

            //Get the MIME Type
                string mime = "application/octet-stream"; //DEFAULT UNKNOWN MIME TYPE

                try
                {
            if (file.Take(2).SequenceEqual(BMP))
            {
                mime = "image/bmp";
            }
            else if (file.Take(8).SequenceEqual(DOC))
            {
                mime = "application/msword";
            }
            else if (file.Take(2).SequenceEqual(EXE_DLL))
            {
                        mime = "application/x-msdownload";
            }
            else if (file.Take(4).SequenceEqual(GIF))
            {
                mime = "image/gif";
            }
            else if (file.Take(4).SequenceEqual(ICO))
            {
                mime = "image/x-icon";
            }
            else if (file.Take(3).SequenceEqual(JPG))
            {
                mime = "image/jpeg";
            }
            else if (file.Take(3).SequenceEqual(MP3))
            {
                mime = "audio/mpeg";
            }
            else if (file.Take(14).SequenceEqual(OGG))
            {
                        mime = extension switch
                        {
                            ".OGX" => "application/ogg",
                            ".OGA" => "audio/ogg",
                            _ => "video/ogg"
                        };
            }
            else if (file.Take(7).SequenceEqual(PDF))
            {
                mime = "application/pdf";
            }
            else if (file.Take(16).SequenceEqual(PNG))
            {
                mime = "image/png";
            }
            else if (file.Take(7).SequenceEqual(RAR))
            {
                mime = "application/x-rar-compressed";
            }
            else if (file.Take(3).SequenceEqual(SWF))
            {
                mime = "application/x-shockwave-flash";
            }
            else if (file.Take(4).SequenceEqual(TIFF))
            {
                mime = "image/tiff";
            }
            else if (file.Take(11).SequenceEqual(TORRENT))
            {
                mime = "application/x-bittorrent";
            }
            else if (file.Take(5).SequenceEqual(TTF))
            {
                mime = "application/x-font-ttf";
            }
            else if (file.Take(4).SequenceEqual(WAV_AVI))
            {
                mime = extension == ".AVI" ? "video/x-msvideo" : "audio/x-wav";
            }
            else if (file.Take(16).SequenceEqual(WMV_WMA))
            {
                mime = extension == ".WMA" ? "audio/x-ms-wma" : "video/x-ms-wmv";
            }
            else if (file.Take(4).SequenceEqual(ZIP_DOCX))
            {
                        mime = extension == ".DOCX" ? 
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" : 
                            "application/x-zip-compressed";
            }

                    LOGGER.LogInformation("Detected MIME type {MimeType} for file {FileName}", mime, fileName);
            return mime;
                }
                catch (Exception ex)
                {
                    LOGGER.LogWarning(ex, "Error during MIME type detection for file {FileName}, using default MIME type", fileName);
                    return "application/octet-stream";
                }
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Critical error determining MIME type for file: {FileName}", fileName);
                return "application/octet-stream";
            }
        }

        /// <summary>
        /// This code snippet first extracts the unit from the input string by taking the last two characters of the string,
        /// and then extracts the value by taking all the characters except for the last two. 
        /// Then it checks if the unit is "GB" or not. If it is, it will convert the value from GB to bytes.
        /// </summary>
        public static long ConvertGBToLong(string input)
        {
            try
            {
                LOGGER.LogDebug("Converting GB string to bytes: {Input}", input);

                if (string.IsNullOrWhiteSpace(input))
                {
                    LOGGER.LogWarning("Empty input provided for GB conversion");
                    return 0;
                }

                if (input.Length < 2)
                {
                    LOGGER.LogWarning("Invalid input format for GB conversion: {Input}", input);
                    return 0;
                }

            string unit = input.Substring(input.Length - 2);
                if (!int.TryParse(input.Substring(0, input.Length - 2), out int value))
                {
                    LOGGER.LogWarning("Failed to parse numeric value from input: {Input}", input);
                    return 0;
                }

                if (unit != "GB")
                {
                    LOGGER.LogWarning("Invalid unit in input (expected 'GB'): {Input}", input);
                    return 0;
                }

                long result = value * 1073741824L;
                LOGGER.LogInformation("Successfully converted {Input} to {Bytes} bytes", input, result);
            return result;
            }
            catch (Exception ex)
            {
                LOGGER.LogError(ex, "Error converting GB string to bytes: {Input}", input);
                return 0;
            }
        }

        public class LazilyResolved<T> : Lazy<T>
        {
            private static readonly ILogger<LazilyResolved<T>> LOGGER = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            }).CreateLogger<LazilyResolved<T>>();

            public LazilyResolved(IServiceProvider serviceProvider)
                : base(() =>
                {
                    try
                    {
                        LOGGER.LogDebug("Resolving service of type {ServiceType}", typeof(T).Name);
                        var service = serviceProvider.GetRequiredService<T>();
                        LOGGER.LogDebug("Successfully resolved service of type {ServiceType}", typeof(T).Name);
                        return service;
                    }
                    catch (Exception ex)
                    {
                        LOGGER.LogError(ex, "Error resolving service of type {ServiceType}", typeof(T).Name);
                        throw;
                    }
                })
            {
                if (serviceProvider == null)
                {
                    LOGGER.LogWarning("ServiceProvider is null");
                    throw new ArgumentNullException(nameof(serviceProvider));
                }
            }
        }
    }
}
