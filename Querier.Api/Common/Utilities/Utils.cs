using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        public static DbContext GetDbContextFromTypeName(string contextTypeName)
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

                // Get the connection string from the database
                LOGGER?.LogTrace("Getting connection string from database");
                var apiDbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiDbContext>>();
                using var apiDbContext = apiDbContextFactory.CreateDbContext();
                var connection = apiDbContext.DBConnections.FirstOrDefault(c => c.ContextName == contextTypeName);
                
                if (connection == null)
                {
                    LOGGER?.LogError("No connection found for context: {ContextTypeName}", contextTypeName);
                    throw new InvalidOperationException($"No connection found for context {contextTypeName}");
                }

                // Create options with the correct connection string
                LOGGER?.LogTrace("Creating context options with connection string");
                var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
                var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsBuilderType);

                switch (connection.ConnectionType)
                {
                    case Domain.Common.Enums.DbConnectionType.SqlServer:
                        LOGGER?.LogDebug("Configuring SQL Server connection");
                        if (optionsBuilder != null) optionsBuilder.UseSqlServer(connection.ConnectionString);
                        break;
                    case Domain.Common.Enums.DbConnectionType.MySql:
                        LOGGER?.LogDebug("Configuring MySQL connection");
                        if (optionsBuilder != null)
                            optionsBuilder.UseMySql(connection.ConnectionString,
                                ServerVersion.AutoDetect(connection.ConnectionString));
                        break;
                    case Domain.Common.Enums.DbConnectionType.PgSql:
                        LOGGER?.LogDebug("Configuring PostgresSQL connection");
                        if (optionsBuilder != null) optionsBuilder.UseNpgsql(connection.ConnectionString);
                        break;
                    default:
                        LOGGER?.LogError("Unsupported database type: {ConnectionType}", connection.ConnectionType);
                        throw new NotSupportedException($"Database type {connection.ConnectionType} not supported");
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
    }
}
