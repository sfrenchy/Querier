using Querier.Api.Models.Requests;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly ILogger<ExportController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IExportService _exportService;
        public ExportController(IExportService exportService, IConfiguration configuration, ILogger<ExportController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _exportService = exportService;
        }

        [HttpPost("AskExport")]
        public void AskExport([FromBody] ExportRequest request)
        {
            _exportService.AskExport(request);
        }
    }
}