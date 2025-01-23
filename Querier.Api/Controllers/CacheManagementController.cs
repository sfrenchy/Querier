using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Services;
using StackExchange.Redis;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing application cache
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Managing cache entries
    /// - Clearing cache data
    /// - Monitoring cache status
    /// - Handling cache invalidation
    /// </remarks>
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class CacheManagementController(
        ICacheManagementService cacheManagementService,
        ILogger<CacheManagementController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Clears all cached data
        /// </summary>
        /// <remarks>
        /// This endpoint removes all entries from the cache.
        /// Use with caution as it affects application performance until the cache is rebuilt.
        /// 
        /// Sample request:
        ///     GET /api/v1/cachemanagement/flushall
        /// </remarks>
        /// <returns>No content on success</returns>
        /// <response code="200">Cache was successfully cleared</response>
        /// <response code="503">Redis server is unavailable</response>
        [HttpGet]
        [Route("FlushAll")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> FlushAllAsync()
        {
            try
            {
                logger.LogInformation("Request received to flush all cache");
                await cacheManagementService.FlushAllAsync();
                logger.LogInformation("Successfully flushed all cache");
                return Ok(new { message = "Cache successfully cleared" });
            }
            catch (RedisConnectionException ex)
            {
                logger.LogError(ex, "Redis server is unavailable");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                    new { error = "Cache service is currently unavailable" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while flushing all cache");
                throw;
            }
        }

        /// <summary>
        /// Clears cache entries containing the specified substring
        /// </summary>
        /// <remarks>
        /// Removes all cache entries where the key contains the provided substring.
        /// 
        /// Sample request:
        ///     GET /api/v1/cachemanagement/flushbysubstring?substring=user
        /// </remarks>
        /// <param name="substring">The substring to match against cache keys</param>
        /// <returns>No content on success</returns>
        /// <response code="200">Matching cache entries were successfully cleared</response>
        /// <response code="400">Substring parameter is null or empty</response>
        /// <response code="503">Redis server is unavailable</response>
        [HttpGet]
        [Route("FlushBySubstring")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> FlushBySubstringAsync([FromQuery] string substring)
        {
            if (string.IsNullOrEmpty(substring))
            {
                logger.LogWarning("Attempt to flush cache with null or empty substring");
                return BadRequest(new { error = "Substring parameter cannot be null or empty" });
            }

            try
            {
                logger.LogInformation("Request received to flush cache entries containing substring: {Substring}", substring);
                await cacheManagementService.FlushBySubstringAsync(substring);
                logger.LogInformation("Successfully flushed cache entries containing substring: {Substring}", substring);
                return Ok(new { message = $"Cache entries containing '{substring}' successfully cleared" });
            }
            catch (RedisConnectionException ex)
            {
                logger.LogError(ex, "Redis server is unavailable while flushing by substring: {Substring}", substring);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                    new { error = "Cache service is currently unavailable" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while flushing cache entries with substring: {Substring}", substring);
                throw;
            }
        }

        /// <summary>
        /// Clears a specific cache entry by its key
        /// </summary>
        /// <remarks>
        /// Removes a single cache entry that exactly matches the provided key.
        /// 
        /// Sample request:
        ///     GET /api/v1/cachemanagement/flushbykey?key=user:123
        /// </remarks>
        /// <param name="key">The exact cache key to remove</param>
        /// <returns>No content on success</returns>
        /// <response code="200">Cache entry was successfully cleared</response>
        /// <response code="400">Key parameter is null or empty</response>
        /// <response code="503">Redis server is unavailable</response>
        [HttpGet]
        [Route("FlushByKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> FlushByKeyAsync([FromQuery] string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                logger.LogWarning("Attempt to flush cache with null or empty key");
                return BadRequest(new { error = "Key parameter cannot be null or empty" });
            }

            try
            {
                logger.LogInformation("Request received to flush cache entry with key: {Key}", key);
                await cacheManagementService.FlushByKeyAsync(key);
                logger.LogInformation("Successfully flushed cache entry with key: {Key}", key);
                return Ok(new { message = $"Cache entry with key '{key}' successfully cleared" });
            }
            catch (RedisConnectionException ex)
            {
                logger.LogError(ex, "Redis server is unavailable while flushing key: {Key}", key);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                    new { error = "Cache service is currently unavailable" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while flushing cache entry with key: {Key}", key);
                throw;
            }
        }
    }
}