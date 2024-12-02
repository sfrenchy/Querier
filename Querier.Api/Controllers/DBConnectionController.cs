using Querier.Api.Models;
using Querier.Api.Models.Datatable;
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
using Querier.Api.Models.QDBConnection;
using Querier.Api.Models.Requests;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class DBConnectionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDBConnectionService _dbConnectionService;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<DBConnectionController> _logger;

        public DBConnectionController(IHostApplicationLifetime hostApplicationLifetime, IDBConnectionService dbConnectionService, IConfiguration configuration, ILogger<DBConnectionController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbConnectionService = dbConnectionService;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        [HttpPost("AddDbConnection")]
        public async Task<IActionResult> AddDBConnectionAsync([FromBody] AddDBConnectionRequest connection)
        {
            return Ok(await _dbConnectionService.AddConnectionAsync(connection));
        }

        [HttpPost("ReadDBConnection")]
        public async Task<IActionResult> ReadDBConnectionAsync(ServerSideRequest request)
        {
            return Ok(await _dbConnectionService.ReadDBConnectionAsync(request));
        }

        [HttpDelete("DeleteDBConnection")]
        public async Task<IActionResult> DeleteDBConnectionAsync(DeleteDBConnectionRequest request)
        {
            return Ok(await _dbConnectionService.DeleteDBConnectionAsync(request));
        }
    }
}
