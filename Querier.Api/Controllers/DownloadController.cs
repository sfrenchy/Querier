using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for handling file download operations
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Downloading generated reports
    /// - Managing file downloads with optional automatic deletion
    /// 
    /// ## Authentication
    /// All endpoints in this controller require authentication.
    /// Use a valid JWT token in the Authorization header:
    /// ```
    /// Authorization: Bearer {your-jwt-token}
    /// ```
    /// 
    /// ## Common Responses
    /// - 200 OK: File download successful
    /// - 401 Unauthorized: Authentication required
    /// - 403 Forbidden: User lacks required permissions
    /// - 404 Not Found: Requested file not found
    /// - 500 Internal Server Error: Unexpected server error
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class DownloadController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(IConfiguration configuration, ILogger<DownloadController> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Downloads a report file by its identifier
        /// </summary>
        /// <remarks>
        /// Retrieves a file from the downloads directory and optionally deletes it after download
        /// based on the application configuration.
        /// 
        /// Sample request:
        ///     GET /api/v1/download/downloadreport?id=report_123.pdf
        /// 
        /// The file will be deleted after download if 'ApplicationSettings:ReportDeleteOnDownload' 
        /// is set to 'true' in the configuration.
        /// </remarks>
        /// <param name="id">The filename or identifier of the report to download</param>
        /// <returns>The file content with appropriate content type</returns>
        /// <response code="200">Returns the requested file</response>
        /// <response code="404">If the file was not found</response>
        [HttpGet("DownloadReport")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
