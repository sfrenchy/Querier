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
    [Route("api/v1/[controller]")]
    [ApiController]
    public class DownloadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(IConfiguration configuration, ILogger<DownloadController> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("DownloadReport")]
        public async Task<ActionResult> DownloadReport([FromQuery] string id)
        {
            var filePath = $"wwwroot/downloads/{id}";
            // ... code for validation and get the file

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(filePath, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            byte[] bytes;
            if (System.IO.File.Exists(filePath))
            {
                bytes = await System.IO.File.ReadAllBytesAsync(filePath);
                if (_configuration["ApplicationSettings:ReportDeleteOnDownload"] == "true")
                {
                    System.IO.File.Delete(filePath);
                }
            }
            else
            {
                bytes = new byte[] { };
            }
            return File(bytes, contentType, Path.GetFileName(filePath));
        }
    }
}
