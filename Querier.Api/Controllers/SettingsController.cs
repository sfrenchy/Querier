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
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests;
using Microsoft.AspNetCore.Http;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Settings controller for managing application configuration
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Retrieving application settings
    /// - Updating application settings
    /// - Checking application configuration status
    /// 
    /// ## Authentication
    /// Most endpoints in this controller require authentication except for the `/configured` endpoint.
    /// Use a valid JWT token in the Authorization header:
    /// ```
    /// Authorization: Bearer {your-jwt-token}
    /// ```
    /// 
    /// ## Common Responses
    /// - 200 OK: Operation completed successfully
    /// - 401 Unauthorized: Authentication required or token invalid
    /// - 403 Forbidden: User lacks required permissions
    /// - 500 Internal Server Error: Unexpected server error
    /// </remarks>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user is not authorized</response>
    /// <response code="500">If there was an unexpected server error</response>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class SettingsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ISettingService _settingService;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(IConfiguration configuration, ISettingService settingService, ILogger<SettingsController> logger)
        {
            _configuration = configuration;
            _settingService = settingService;
            _logger = logger;
        }

        /// <summary>
        /// Get all application settings
        /// </summary>
        /// <remarks>   
        /// This endpoint is used to retrieve all application settings. It requires authentication.
        /// 
        /// Sample request:
        ///     GET /api/v1/settings
        /// </remarks>
        /// <returns>A list of application settings</returns>
        /// <response code="200">Returns the list of application settings</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not authorized</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _settingService.GetSettings();
            return Ok(settings);
        }

        /// <summary>
        /// Update an application setting
        /// </summary>
        /// <remarks>
        /// This endpoint is used to update an application setting. It requires authentication.
        /// 
        /// Sample request:
        ///     POST /api/v1/settings
        ///     {
        ///         "name": "isConfigured",
        ///         "value": "true"
        ///     }
        /// </remarks>
        /// <param name="setting"></param>
        /// <returns>The updated setting</returns>
        /// <response code="200">Returns the updated setting</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not authorized</response>
        /// <response code="500">If there was an internal server error</response>
        
        [HttpPost]
        public async Task<IActionResult> UpdateSettings([FromBody] QSetting setting)
        {
            var updatedSetting = await _settingService.UpdateSetting(setting);
            return Ok(updatedSetting);
        }
        /// <summary>
        /// Configure an application setting
        /// </summary>
        /// <remarks>   
        /// This endpoint is used to configure an application setting. It requires authentication.
        /// 
        /// Sample request:
        ///     POST /api/v1/settings/configure
        ///     {
        ///         "name": "isConfigured",
        ///         "value": "true"
        ///     }
        /// </remarks>
        /// <param name="setting"></param>
        /// <returns>The updated setting</returns>
        /// <response code="200">Returns the updated setting</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost("configure")]
        public async Task<IActionResult> Configure([FromBody] QSetting setting)
        {
            var configuredSetting = await _settingService.Configure(setting);
            return Ok(configuredSetting);
        }   
    }
}