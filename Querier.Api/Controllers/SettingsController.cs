using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Collections.Generic;
using Querier.Api.Application.DTOs.Common.ApiConfigurationDto;
using Querier.Api.Domain.Common.Metadata;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Services;

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
        private readonly UserManager<ApiUser> _userManager;

        public SettingsController(
            IConfiguration configuration, 
            ISettingService settingService, 
            ILogger<SettingsController> logger,
            UserManager<ApiUser> userManager)
        {
            _configuration = configuration;
            _settingService = settingService;
            _logger = logger;
            _userManager = userManager;
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
        ///         "name": "api:isConfigured",
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
        ///         "name": "api:isConfigured",
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

        /// <summary>
        /// Get API configuration settings
        /// </summary>
        /// <remarks>
        /// This endpoint retrieves the API configuration including scheme, host, port and CORS settings.
        /// Only users with the Admin role can access this endpoint.
        /// 
        /// Sample request:
        ///     GET /api/v1/settings/api-configuration
        /// 
        /// Sample response:
        ///     {
        ///         "scheme": "https",
        ///         "host": "localhost",
        ///         "port": 5001,
        ///         "allowedHosts": "*",
        ///         "allowedOrigins": "*",
        ///         "allowedMethods": "GET,POST,DELETE,OPTIONS",
        ///         "allowedHeaders": "X-Request-Token,Accept,Content-Type,Authorization"
        ///     }
        /// </remarks>
        /// <returns>The API configuration settings</returns>
        /// <response code="200">Returns the API configuration</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not an Admin</response>
        /// <response code="500">If there was an error retrieving the configuration</response>
        [Authorize]
        [HttpGet("api-configuration")]
        public async Task<ActionResult<ApiConfigurationDto>> GetApiConfiguration()
        {
            try
            {
                // Ajouter ces logs pour voir tous les claims
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation("Claim: Type = {Type}, Value = {Value}", 
                        claim.Type, claim.Value);
                }

                // Vérifier les claims
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                
                _logger.LogInformation("Claims - Email: {Email}", userEmail);

                var user = await _userManager.FindByEmailAsync(userEmail);

                if (user == null)
                {
                    _logger.LogWarning("User not found with Email: {Email}", userEmail);
                    return Forbid();
                }

                var roles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("User {Email} has roles: {Roles}", user.Email, string.Join(", ", roles));

                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (!isAdmin)
                {
                    _logger.LogWarning("User {Email} is not an admin", user.Email);
                    return Forbid();
                }

                var config = new ApiConfigurationDto
                {
                    Scheme = await _settingService.GetSettingValue("api:scheme", "https"),
                    Host = await _settingService.GetSettingValue("api:host", "localhost"),
                    Port = int.Parse(await _settingService.GetSettingValue("api:port", "5001")),
                    AllowedHosts = await _settingService.GetSettingValue("api:allowedHosts", "*"),
                    AllowedOrigins = await _settingService.GetSettingValue("api:allowedOrigins", "*"),
                    AllowedMethods = await _settingService.GetSettingValue("api:allowedMethods", "GET,POST,DELETE,OPTIONS"),
                    AllowedHeaders = await _settingService.GetSettingValue("api:allowedHeaders", "X-Request-Token,Accept,Content-Type,Authorization"),
                    ResetPasswordTokenValidity = int.Parse(await _settingService.GetSettingValue("api:email:ResetPasswordTokenValidityLifeSpanMinutes", "15")),
                    EmailConfirmationTokenValidity = int.Parse(await _settingService.GetSettingValue("api:email:confirmationTokenValidityLifeSpanDays", "2")),
                    RequireDigit = bool.Parse(await _settingService.GetSettingValue("api:password:requireDigit", "true")),
                    RequireLowercase = bool.Parse(await _settingService.GetSettingValue("api:password:requireLowercase", "true")),
                    RequireNonAlphanumeric = bool.Parse(await _settingService.GetSettingValue("api:password:requireNonAlphanumeric", "true")),
                    RequireUppercase = bool.Parse(await _settingService.GetSettingValue("api:password:requireUppercase", "true")),
                    RequiredLength = int.Parse(await _settingService.GetSettingValue("api:password:requiredLength", "12")),
                    RequiredUniqueChars = int.Parse(await _settingService.GetSettingValue("api:password:requiredUniqueChars", "1")),
                    SmtpHost = await _settingService.GetSettingValue("api:smtp:host", ""),
                    SmtpPort = int.Parse(await _settingService.GetSettingValue("api:smtp:port", "587")),
                    SmtpUsername = await _settingService.GetSettingValue("api:smtp:username", ""),
                    SmtpPassword = await _settingService.GetSettingValue("api:smtp:password", ""),
                    SmtpUseSSL = bool.Parse(await _settingService.GetSettingValue("api:smtp:useSSL", "true")),
                    SmtpSenderEmail = await _settingService.GetSettingValue("api:smtp:senderEmail", ""),
                    SmtpSenderName = await _settingService.GetSettingValue("api:smtp:senderName", ""),
                    SmtpRequireAuth = bool.Parse(await _settingService.GetSettingValue("api:smtp:requiresAuth", "true")),
                    RedisEnabled = bool.Parse(await _settingService.GetSettingValue("api:redis:enabled", "false")),
                    RedisHost = await _settingService.GetSettingValue("api:redis:host", "localhost"),
                    RedisPort = int.Parse(await _settingService.GetSettingValue("api:redis:port", "6379")),
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API configuration");
                return StatusCode(500, "Error retrieving API configuration");
            }
        }

        /// <summary>
        /// Update API configuration settings
        /// </summary>
        /// <remarks>
        /// This endpoint is used to update the API configuration settings. Only users with the Admin role can access this endpoint.
        /// 
        /// Sample request:
        ///     POST /api/v1/settings/api-configuration
        ///     {
        ///         "scheme": "https",
        ///         "host": "localhost",
        ///         "port": 5001,
        ///         "allowedHosts": "*",
        ///         "allowedOrigins": "*",
        ///         "allowedMethods": "GET,POST,DELETE,OPTIONS",
        ///         "allowedHeaders": "X-Request-Token,Accept,Content-Type,Authorization",
        ///         "resetPasswordTokenValidity": 15,
        ///         "emailConfirmationTokenValidity": 2,
        ///         "requireDigit": true,
        ///         "requireLowercase": true,
        ///         "requireNonAlphanumeric": true,
        ///         "requireUppercase": true,
        ///         "requiredLength": 12,
        ///         "requiredUniqueChars": 1
        ///     }
        /// </remarks>
        /// <param name="config"></param>
        /// <returns>The updated API configuration</returns>
        /// <response code="200">Returns the updated API configuration</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not an Admin</response>
        /// <response code="500">If there was an error updating the configuration</response>
        [HttpPost("api-configuration")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateApiConfiguration([FromBody] ApiConfigurationDto config)
        {
            try
            {
                var settings = new Dictionary<string, string>
                {
                    { "api:scheme", config.Scheme },
                    { "api:host", config.Host },
                    { "api:port", config.Port.ToString() },
                    { "api:allowedHosts", config.AllowedHosts },
                    { "api:allowedOrigins", config.AllowedOrigins },
                    { "api:allowedMethods", config.AllowedMethods },
                    { "api:allowedHeaders", config.AllowedHeaders },
                    { "api:email:ResetPasswordTokenValidityLifeSpanMinutes", config.ResetPasswordTokenValidity.ToString() },
                    { "api:email:confirmationTokenValidityLifeSpanDays", config.EmailConfirmationTokenValidity.ToString() },
                    { "api:password:requireDigit", config.RequireDigit.ToString() },
                    { "api:password:requireLowercase", config.RequireLowercase.ToString() },
                    { "api:password:requireNonAlphanumeric", config.RequireNonAlphanumeric.ToString() },
                    { "api:password:requireUppercase", config.RequireUppercase.ToString() },
                    { "api:password:requiredLength", config.RequiredLength.ToString() },
                    { "api:password:requiredUniqueChars", config.RequiredUniqueChars.ToString() },
                    { "api:smtp:host", config.SmtpHost },
                    { "api:smtp:port", config.SmtpPort.ToString() },
                    { "api:smtp:username", config.SmtpUsername },
                    { "api:smtp:password", config.SmtpPassword },
                    { "api:smtp:useSSL", config.SmtpUseSSL.ToString() },
                    { "api:smtp:senderEmail", config.SmtpSenderEmail },
                    { "api:smtp:senderName", config.SmtpSenderName },
                    { "api:smtp:requiresAuth", config.SmtpRequireAuth.ToString() },
                    { "api:redis:enabled", config.RedisEnabled.ToString() },
                    { "api:redis:host", config.RedisHost },
                    { "api:redis:port", config.RedisPort.ToString() },
                };

                await _settingService.UpdateSettings(settings);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API configuration");
                return StatusCode(500, "Error updating API configuration");
            }
        }
    }
}