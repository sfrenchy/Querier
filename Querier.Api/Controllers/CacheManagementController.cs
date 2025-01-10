using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Services;

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
    public class CacheManagementController : ControllerBase
    {
        private readonly ICacheManagementService _cacheManagementService;
        private readonly ILogger<CacheManagementService> _logger;

        public CacheManagementController(ICacheManagementService cacheManagementService, ILogger<CacheManagementService> logger)
        {
            _cacheManagementService = cacheManagementService;
            _logger = logger;
        }

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
        [HttpGet]
        [Route("FlushAll")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task FlushAllAsync()
        {
            await _cacheManagementService.FlushAllAsync();
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
        [HttpGet]
        [Route("FlushBySubstring")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task FlushBySubstringAsync(string substring)
        {
            await _cacheManagementService.FlushBySubstringAsync(substring);
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
        [HttpGet]
        [Route("FlushByKey")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task FlushByKeyAsync(string key)
        {
            await _cacheManagementService.FlushByKeyAsync(key);
        }
    }
}