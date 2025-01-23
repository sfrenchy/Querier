using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Services;
using StackExchange.Redis;

namespace Querier.Api.Domain.Services
{
    public class CacheManagementService : ICacheManagementService
    {
        private readonly ILogger<CacheManagementService> _logger;
        private readonly ConfigurationOptions _redisOptions;

        public CacheManagementService(IConfiguration configuration, ILogger<CacheManagementService> logger)
        {
            _logger = logger;
            
            var redisUrl = configuration["RedisCacheUrl"];
            if (string.IsNullOrEmpty(redisUrl))
            {
                throw new InvalidOperationException("Redis cache URL is not configured");
            }

            _redisOptions = ConfigurationOptions.Parse(redisUrl);
            _redisOptions.AllowAdmin = true;
        }

        public async Task FlushAllAsync()
        {
            try
            {
                _logger.LogInformation("Starting flush of all cache databases");
                await using var redis = await ConnectionMultiplexer.ConnectAsync(_redisOptions);
                var endpoints = redis.GetEndPoints();
                
                if (endpoints.Length == 0)
                {
                    throw new InvalidOperationException("No Redis endpoints available");
                }

                var server = redis.GetServer(endpoints[0]);
                await server.FlushAllDatabasesAsync();
                _logger.LogInformation("Successfully flushed all cache databases");
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis server");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while flushing all cache databases");
                throw;
            }
        }

        public async Task FlushBySubstringAsync(string substring)
        {
            if (string.IsNullOrEmpty(substring))
            {
                throw new ArgumentException("Substring cannot be null or empty", nameof(substring));
            }

            try
            {
                _logger.LogInformation("Starting flush of cache entries containing substring: {Substring}", substring);
                await using var redis = await ConnectionMultiplexer.ConnectAsync(_redisOptions);
                var endpoints = redis.GetEndPoints();
                
                if (endpoints.Length == 0)
                {
                    throw new InvalidOperationException("No Redis endpoints available");
                }

                var server = redis.GetServer(endpoints[0]);
                var db = redis.GetDatabase();
                var pattern = $"*{substring}*";
                var keys = server.Keys(pattern: pattern);
                var keyCount = 0;

                foreach (var key in keys)
                {
                    await db.KeyDeleteAsync(key);
                    keyCount++;
                }

                _logger.LogInformation("Successfully deleted {Count} cache entries containing substring: {Substring}", 
                    keyCount, substring);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis server while flushing by substring: {Substring}", 
                    substring);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while flushing cache entries with substring: {Substring}", 
                    substring);
                throw;
            }
        }

        public async Task FlushByKeyAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            try
            {
                _logger.LogInformation("Starting flush of cache entry with key: {Key}", key);
                await using var redis = await ConnectionMultiplexer.ConnectAsync(_redisOptions);
                var db = redis.GetDatabase();
                
                var deleted = await db.KeyDeleteAsync(key);
                if (deleted)
                {
                    _logger.LogInformation("Successfully deleted cache entry with key: {Key}", key);
                }
                else
                {
                    _logger.LogWarning("No cache entry found with key: {Key}", key);
                }
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis server while flushing key: {Key}", key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while flushing cache entry with key: {Key}", key);
                throw;
            }
        }
    }
}
