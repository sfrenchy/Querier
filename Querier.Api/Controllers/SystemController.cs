using Querier.Api.Models;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SystemController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDBConnectionService _dbConnectionService;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<SystemController> _logger;

        public SystemController(IHostApplicationLifetime hostApplicationLifetime, ILogger<SystemController> logger)
        { 
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        [HttpGet("StopApplication")]
        public IActionResult StopApplication()
        {
            // TODO: Check if user is admin
            _hostApplicationLifetime.StopApplication();
            return Ok();
        }
    }
}
