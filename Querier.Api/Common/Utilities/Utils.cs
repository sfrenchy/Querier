using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Infrastructure.Data.Context;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Querier.Api.Common.Utilities
{
    public static class Utils
    {
        private static readonly ILogger LOGGER;

        static Utils()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            LOGGER = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(Utils));
        }

        public static DbContext GetDbContextFromTypeName(string contextTypeName, string connectionString = null, DbConnectionType connectionType = DbConnectionType.SqlServer)
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeName))
                {
                    LOGGER?.LogError("Context type name is null or empty");
                    throw new ArgumentException("Context type name cannot be null or empty", nameof(contextTypeName));
                }

                LOGGER?.LogDebug("Searching for DbContext with type name: {ContextTypeName}", contextTypeName);
                var contextTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == contextTypeName)
                    .ToList();

                if (!contextTypes.Any())
                {
                    LOGGER?.LogError("No DbContext found with type name: {ContextTypeName}", contextTypeName);
                    throw new InvalidOperationException($"No DbContext found with type name {contextTypeName}");
                }

                var contextType = contextTypes.First();
                var scope = ServiceActivator.GetScope();
                
                // Try to get from DI first
                LOGGER?.LogTrace("Attempting to get context from DI container");
                if (scope.ServiceProvider.GetService(contextType) is DbContext context)
                {
                    LOGGER?.LogDebug("Successfully retrieved context from DI container");
                    return context;
                }

                if (connectionString == null)
                {
                    // Get the connection string from the database
                    LOGGER?.LogTrace("Getting connection string from database");
                    var apiDbContextFactory =
                        scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiDbContext>>();
                    using var apiDbContext = apiDbContextFactory.CreateDbContext();
                    var dbConnection = apiDbContext.DBConnections.FirstOrDefault(c => c.ContextName == contextTypeName);

                    if (dbConnection == null)
                    {
                        LOGGER?.LogError("No connection found for context: {ContextTypeName}", contextTypeName);
                        throw new InvalidOperationException($"No connection found for context {contextTypeName}");
                    }
                    connectionString = dbConnection.ConnectionString;
                    connectionType = dbConnection.ConnectionType;
                }

                // Create options with the correct connection string
                LOGGER?.LogTrace("Creating context options with connection string");
                var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
                var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsBuilderType);

                switch (connectionType)
                {
                    case DbConnectionType.SqlServer:
                        LOGGER?.LogDebug("Configuring SQL Server connection");
                        if (optionsBuilder != null) optionsBuilder.UseSqlServer(connectionString);
                        break;
                    case DbConnectionType.MySql:
                        LOGGER?.LogDebug("Configuring MySQL connection");
                        if (optionsBuilder != null)
                            optionsBuilder.UseMySql(connectionString,
                                ServerVersion.AutoDetect(connectionString));
                        break;
                    case DbConnectionType.PgSql:
                        LOGGER?.LogDebug("Configuring PostgresSQL connection");
                        if (optionsBuilder != null) optionsBuilder.UseNpgsql(connectionString);
                        break;
                    default:
                        LOGGER?.LogError("Unsupported database type: {ConnectionType}", connectionType);
                        throw new NotSupportedException($"Database type {connectionType} not supported");
                }

                LOGGER?.LogDebug("Creating new instance of context type: {ContextType}", contextType.Name);
                if (optionsBuilder != null)
                    return (DbContext)Activator.CreateInstance(contextType, optionsBuilder.Options);
            }
            catch (Exception ex) when (ex is not InvalidOperationException && ex is not NotSupportedException)
            {
                LOGGER?.LogError(ex, "Error creating DbContext for type name: {ContextTypeName}", contextTypeName);
                throw;
            }

            return null;
        }

        public static string RandomString(int length)
        {
            try
            {
                if (length <= 0)
                {
                    LOGGER?.LogError("Invalid length for random string: {Length}", length);
                    throw new ArgumentException("Length must be greater than 0", nameof(length));
                }

                LOGGER?.LogTrace("Generating random string of length {Length}", length);
                var random = new Random();
                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                var result = new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
                
                LOGGER?.LogTrace("Successfully generated random string");
                return result;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                LOGGER?.LogError(ex, "Error generating random string of length {Length}", length);
                throw;
            }
        }

        public static Type GetType(string typeName)
        {
            try
            {
                if (string.IsNullOrEmpty(typeName))
                {
                    LOGGER?.LogError("Type name is null or empty");
                    throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));
                }

                LOGGER?.LogTrace("Searching for type: {TypeName}", typeName);
                var type = Type.GetType(typeName);
                if (type != null)
                {
                    LOGGER?.LogDebug("Found type directly: {TypeName}", typeName);
                    return type;
                }

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        LOGGER?.LogDebug("Found type in assembly {Assembly}: {TypeName}", 
                            assembly.GetName().Name, typeName);
                        return type;
                    }
                }

                LOGGER?.LogWarning("Type not found: {TypeName}", typeName);
                return null;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                LOGGER?.LogError(ex, "Error searching for type: {TypeName}", typeName);
                throw;
            }
        }

        public static string ComputeMd5Hash(string str)
        {
            return ComputeMd5Hash(Encoding.UTF8.GetBytes(str));
        }
        public static string ComputeMd5Hash(byte[] objectAsBytes)
        {
            if (objectAsBytes == null)
            {
                LOGGER?.LogError("Input bytes array is null");
                throw new ArgumentNullException(nameof(objectAsBytes));
            }

            LOGGER?.LogTrace("Computing MD5 hash for byte array of length {Length}", objectAsBytes.Length);
            using var md5 = MD5.Create();
            try
            {
                byte[] result = md5.ComputeHash(objectAsBytes);
                StringBuilder sb = new StringBuilder();
                
                foreach (var t in result)
                {
                    sb.Append(t.ToString("X2"));
                }

                LOGGER?.LogTrace("Successfully computed MD5 hash");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                LOGGER?.LogError(ex, "Error computing MD5 hash");
                return null;
            }
        }

        public static byte[] ObjectToByteArray(Object objectToSerialize)
        {
            try
            {
                if (objectToSerialize == null)
                {
                    LOGGER?.LogError("Input object is null");
                    throw new ArgumentNullException(nameof(objectToSerialize));
                }

                LOGGER?.LogTrace("Serializing object to byte array");
                var json = JsonSerializer.Serialize(objectToSerialize);
                var result = Encoding.ASCII.GetBytes(json);
                
                LOGGER?.LogTrace("Successfully serialized object to byte array of length {Length}", result.Length);
                return result;
            }
            catch (Exception ex) when (ex is not ArgumentNullException)
            {
                LOGGER?.LogError(ex, "Error serializing object to byte array");
                throw;
            }
        }
        
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            try
            {
                LOGGER?.LogTrace("Converting Unix timestamp {Timestamp} to DateTime", unixTimeStamp);
                DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
                
                LOGGER?.LogTrace("Successfully converted Unix timestamp to DateTime: {DateTime}", dtDateTime);
                return dtDateTime;
            }
            catch (Exception ex)
            {
                LOGGER?.LogError(ex, "Error converting Unix timestamp {Timestamp} to DateTime", unixTimeStamp);
                throw;
            }
        }

        public static DataPagedResult<T> ApplyDataRequestParametersDto<T>(this IQueryable<T> query,
            DataRequestParametersDto? dataRequestParameters)
        {
            if (dataRequestParameters == null)
            {
                LOGGER?.LogError("Data request parameters is null");
                throw new ArgumentNullException(nameof(dataRequestParameters));
            }
            // Apply search filters
            if (!string.IsNullOrEmpty(dataRequestParameters.GlobalSearch))
            {
                LOGGER?.LogDebug("Applying global search filter: {Search}", dataRequestParameters.GlobalSearch);
                // Load data in memory for complex search operations
                var searchTerm = dataRequestParameters.GlobalSearch.ToLower();
                var searchResults = query.AsEnumerable()
                    .Where(e => e.GetType()
                        .GetProperties()
                        .Where(p => p.PropertyType == typeof(string))
                        .Any(p => ((string)p.GetValue(e, null) ?? string.Empty)
                            .ToLower()
                            .Contains(searchTerm)))
                    .AsQueryable();
                query = searchResults;
            }

            // Apply column-specific searches
            if (dataRequestParameters.ColumnSearches?.Any() == true)
            {
                LOGGER?.LogDebug("Applying column-specific searches");
                var searchResults = query.AsEnumerable();
                foreach (var columnSearch in dataRequestParameters.ColumnSearches)
                {
                    var searchTerm = columnSearch.Value.ToLower();
                    var columnName = columnSearch.Column;
                    searchResults = searchResults.Where(e =>
                        ((string)e.GetType().GetProperty(columnName)?.GetValue(e, null) ?? string.Empty)
                        .ToLower()
                        .Contains(searchTerm));
                }

                query = searchResults.AsQueryable();
            }

            // Apply sorting
            var sortedData = query.AsEnumerable();
            
            if (dataRequestParameters.OrderBy?.Any() == true)
            {
                LOGGER?.LogDebug("Applying sorting");
                var isFirst = true;
                IOrderedEnumerable<T> orderedData = null;

                foreach (var orderBy in dataRequestParameters.OrderBy)
                {
                    if (isFirst)
                    {
                        orderedData = orderBy.IsDescending
                            ? sortedData.OrderByDescending(e => GetPropertyValue(e, orderBy.Column))
                            : sortedData.OrderBy(e => GetPropertyValue(e, orderBy.Column));
                        isFirst = false;
                    }
                    else
                    {
                        orderedData = orderBy.IsDescending
                            ? orderedData.ThenByDescending(e => GetPropertyValue(e, orderBy.Column))
                            : orderedData.ThenBy(e => GetPropertyValue(e, orderBy.Column));
                    }
                }

                sortedData = orderedData ?? sortedData;
            }
            
            var totalCount = sortedData.Count();
            LOGGER?.LogDebug("Total count before pagination: {Count}", totalCount);
            
            // Apply pagination
            var data = sortedData
                .Skip(dataRequestParameters.PageNumber != 0
                    ? (dataRequestParameters.PageNumber - 1) * dataRequestParameters.PageSize
                    : 0)
                .Take(dataRequestParameters.PageNumber != 0 ? dataRequestParameters.PageSize : totalCount);
            var result = new DataPagedResult<T>(data, totalCount, dataRequestParameters);
            return result;
        }
        
        public static object GetPropertyValue(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null) ?? DBNull.Value;
        }

        public static string FormatForeignKeyValue(object entity, ForeignKeyIncludeDto includeConfig)
        {
            try
            {
                if (entity == null)
                {
                    LOGGER?.LogWarning("Entity is null when formatting foreign key value");
                    return string.Empty;
                }

                if (!string.IsNullOrEmpty(includeConfig.DisplayFormat))
                {
                    LOGGER?.LogTrace("Formatting using display format: {Format}", includeConfig.DisplayFormat);
                    // Remplacer les placeholders {PropertyName} par les valeurs des propriétés
                    return System.Text.RegularExpressions.Regex.Replace(
                        includeConfig.DisplayFormat,
                        @"\{([^}]+)\}",
                        match =>
                        {
                            var propertyName = match.Groups[1].Value;
                            var value = GetPropertyValue(entity, propertyName);
                            return value?.ToString() ?? string.Empty;
                        });
                }
                
                if (includeConfig.DisplayColumns?.Any() == true)
                {
                    LOGGER?.LogTrace("Formatting using display columns: {Columns}", 
                        string.Join(", ", includeConfig.DisplayColumns));
                    // Concaténer les valeurs des colonnes spécifiées
                    return string.Join(" ", includeConfig.DisplayColumns
                        .Select(col => GetPropertyValue(entity, col)?.ToString() ?? string.Empty)
                        .Where(v => !string.IsNullOrEmpty(v)));
                }

                LOGGER?.LogWarning("No display format or columns specified for foreign key formatting");
                return entity.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                LOGGER?.LogError(ex, "Error formatting foreign key value");
                return string.Empty;
            }
        }
    }
}
