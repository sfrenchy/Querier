using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Querier.Api.Infrastructure.Data.Context;
using Microsoft.Extensions.DependencyInjection;
using Querier.Api.Common.Utilities;

namespace Querier.Api.Tools
{
    public static class Utils
    {
        public static DbContext GetDbContextFromTypeName(string contextTypeName)
        {
            var contextTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == contextTypeName)
                .ToList();

            if (!contextTypes.Any())
                throw new InvalidOperationException($"No DbContext found with type name {contextTypeName}");

            var contextType = contextTypes.First();
            var scope = ServiceActivator.GetScope();
            
            // Try to get from DI first
            var context = scope.ServiceProvider.GetService(contextType) as DbContext;
            if (context != null)
                return context;

            // Get the connection string from the database
            var apiDbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApiDbContext>>();
            using var apiDbContext = apiDbContextFactory.CreateDbContext();
            var connection = apiDbContext.DBConnections.FirstOrDefault(c => c.ContextName == contextTypeName);
            
            if (connection == null)
                throw new InvalidOperationException($"No connection found for context {contextTypeName}");

            // Create options with the correct connection string
            var optionsBuilderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
            var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(optionsBuilderType);

            switch (connection.ConnectionType)
            {
                case Domain.Common.Enums.DbConnectionType.SqlServer:
                    optionsBuilder.UseSqlServer(connection.ConnectionString);
                    break;
                case Domain.Common.Enums.DbConnectionType.MySql:
                    optionsBuilder.UseMySql(connection.ConnectionString, ServerVersion.AutoDetect(connection.ConnectionString));
                    break;
                case Domain.Common.Enums.DbConnectionType.PgSql:
                    optionsBuilder.UseNpgsql(connection.ConnectionString);
                    break;
                default:
                    throw new NotSupportedException($"Database type {connection.ConnectionType} not supported");
            }

            return (DbContext)Activator.CreateInstance(contextType, optionsBuilder.Options);
        }
        public static string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public static string? ComputeMd5Hash(byte[] objectAsBytes)
        {
            MD5 md5 = MD5.Create();
            try
            {
                byte[] result = md5.ComputeHash(objectAsBytes);

                // Build the final string by converting each byte
                // into hex and appending it to a StringBuilder
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < result.Length; i++)
                {
                    sb.Append(result[i].ToString("X2"));
                }

                // And return it
                return sb.ToString();
            }
            catch (ArgumentNullException ane)
            {
                //If something occurred during serialization, 
                //this method is called with a null argument. 
                Console.WriteLine("Hash has not been generated.");
                return null;
            }
        }

        public static byte[] ObjectToByteArray(Object objectToSerialize)
        {
            return ASCIIEncoding.ASCII.GetBytes(JsonSerializer.Serialize(objectToSerialize));
        }
        
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }
    }
}
