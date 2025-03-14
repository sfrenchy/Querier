using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Querier.Api.Infrastructure.Base.Exceptions;

namespace Querier.Api.Infrastructure.Base;

public abstract class DynamicContextServiceBase<T>
{
    protected string CACHE_VERSION_KEY = "CacheVersionKey";
    protected readonly IDistributedCache _cache;
    protected readonly ILogger<T> _logger;
    protected readonly DistributedCacheEntryOptions _cacheOptions;
    public DynamicContextServiceBase(
        IDistributedCache cache,
        ILogger<T> logger
    )
    {
        _cache = cache;
        _logger = logger;
        _cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        };
    }
    protected async Task<T> ExecuteCacheOperationAsync<T>(string rootNamespace, string operation, string cacheKey, Func<Task<T>> databaseOperation)
    {
        try
        {
            var cachedData = await _cache.GetAsync(cacheKey);
            if (cachedData != null)
            {
                _logger.LogDebug("Cache hit for key: " + cacheKey);
                using var stream = new MemoryStream(cachedData);
                using var reader = new StreamReader(stream);
                return JsonConvert.DeserializeObject<T>(await reader.ReadToEndAsync())
                    ?? throw new CacheOperationException(rootNamespace, "Deserialize", "Failed to deserialize cached data");
            }

            _logger.LogDebug("Cache miss for key: " + cacheKey);
            var startTime = DateTime.UtcNow;

            var result = await databaseOperation();

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug("Operation executed in " + executionTime.ToString() + "ms");

            if (result != null)
            {
                try
                {
                    var serializedData = JsonConvert.SerializeObject(result);
                    await _cache.SetAsync(
                        cacheKey,
                        System.Text.Encoding.UTF8.GetBytes(serializedData),
                        _cacheOptions
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache result for key: " + cacheKey);
                }
            }

            return result;
        }
        catch (Exception ex) when (ex is not DynamicContextException)
        {
            throw new CacheOperationException(rootNamespace, operation, "Cache operation failed", ex);
        }
    }
    protected async Task InvalidateCollectionCacheAsync()
    {
        try
        {
            // Increment version to invalidate all cached queries
            var version = await _cache.GetStringAsync(CACHE_VERSION_KEY) ?? "0";
            var newVersion = (int.Parse(version) + 1).ToString();
            await _cache.SetStringAsync(CACHE_VERSION_KEY, newVersion, _cacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate collection cache");
        }
    }
}
