using Querier.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheManagementController : ControllerBase
    {
        private readonly ILogger<CacheManagementService> _logger;
        private readonly ICacheManagementService _cacheManagementService;
        public CacheManagementController(ICacheManagementService cacheManagementService, ILogger<CacheManagementService> logger)
        {
            _cacheManagementService = cacheManagementService;
            _logger = logger;

        }

        [HttpGet]
        [Route("FlushAll")]
        public async Task FlushAllAsync()
        {
            await _cacheManagementService.FlushAllAsync();
        }

        [HttpGet]
        [Route("FlushBySubstring")]
        public async Task FlushBySubstringAsync(string substring)
        {
            await _cacheManagementService.FlushBySubstringAsync(substring);
        }

        [HttpGet]
        [Route("FlushByKey")]
        public async Task FlushByKeyAsync(string key)
        {
            await _cacheManagementService.FlushByKeyAsync(key);
        }
    }
}
