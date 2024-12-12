using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Querier.Api.Services
{
    /// <summary>
    /// This interface describes all the methods for flushing the  redis cache in different ways.
    /// </summary>
    public interface ICacheManagementService
    {
        /// <summary>
        ///  This method flush all cache.
        /// </summary>
        /// <returns>Return a task</returns>
        Task FlushAllAsync();

        /// <summary>
        ///  this method removes the caches that contain the substring
        /// </summary>
        /// <returns>Return a task</returns>
        /// <param name="substring">the substring that will be taken as an argument</param>
        Task FlushBySubstringAsync(string substring);

        /// <summary>
        ///  this method removes the caches with the key
        /// </summary>
        /// <returns>Return a task</returns>
        /// <param name="key">the key to a cache that will be taken as an argument</param>
        Task FlushByKeyAsync(string key);
    }
    public class CacheManagementService : ICacheManagementService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CacheManagementService> _logger;

        public CacheManagementService(IConfiguration configuration, ILogger<CacheManagementService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task FlushAllAsync()
        {
            _logger.LogInformation("Flushing distributed cache");
            var options = ConfigurationOptions.Parse(_configuration["RedisCacheUrl"]);
            options.AllowAdmin = true;
            var redis = await ConnectionMultiplexer.ConnectAsync(options);
            var endpoints = redis.GetEndPoints();
            var server = redis.GetServer(endpoints[0]);
            await server.FlushAllDatabasesAsync();
        }

        public async Task FlushBySubstringAsync(string substring)
        {
            var options = ConfigurationOptions.Parse(_configuration["RedisCacheUrl"]);
            options.AllowAdmin = true;
            var redis = await ConnectionMultiplexer.ConnectAsync(options);
            var endpoints = redis.GetEndPoints();
            IServer server = redis.GetServer(endpoints[0]);
            IDatabase db = redis.GetDatabase();

            // Define the substring to search for

            // Get all keys matching the substring
            var keys = server.Keys(pattern: "*" + substring + "*");

            // Delete the keys
            foreach (var key in keys)
            {
                db.KeyDelete(key);
            }

            // Close the Redis connection
            redis.Close();
        }

        public async Task FlushByKeyAsync(string key)
        {
            var options = ConfigurationOptions.Parse(_configuration["RedisCacheUrl"]);
            options.AllowAdmin = true;
            var redis = await ConnectionMultiplexer.ConnectAsync(options);

            // Get a reference to the Redis database
            IDatabase db = redis.GetDatabase();

            // Delete the key
            db.KeyDelete(key);

            // Close the Redis connection
            redis.Close();
        }
    }
}
