using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Services;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Settings controller for managing application configuration and API settings
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Managing global application settings
    /// - Configuring API-specific settings
    /// - Handling user-specific configurations
    /// - Managing system-wide settings for authenticated users
    /// 
    /// All endpoints require authentication, and some operations are restricted to users with the Admin role.
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
        /// 
        /// Sample response:
        ///     [
        ///         {
        ///             "name": "api:scheme",
        ///             "value": "https",
        ///             "description": "The scheme of the URL to target API"
        ///         },
        ///         {
        ///             "name": "api:host",
        ///             "value": "localhost",
        ///             "description": "The host of the URL to target API"
        ///         }
        ///     ]
        /// </remarks>
        /// <returns>A list of application settings</returns>
        /// <response code="200">Returns the list of application settings</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not authorized</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SettingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                _logger.LogDebug("Retrieving all settings");
                var settings = await _settingService.GetSettingsAsync();
                _logger.LogInformation("Successfully retrieved {Count} settings", settings?.ToList().Count);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings");
                return StatusCode(500, "An error occurred while retrieving settings");
            }
        }

        /// <summary>
        /// Update an application setting
        /// </summary>
        /// <remarks>
        /// This endpoint is used to update an existing application setting. It requires authentication.
        /// The operation is idempotent - multiple identical requests will have the same effect as a single request.
        /// 
        /// Sample request:
        ///     PUT /api/v1/settings
        ///     {
        ///         "name": "api:isConfigured",
        ///         "value": "true",
        ///         "description": "Flag indicating if the API is configured"
        ///     }
        /// 
        /// Sample response:
        ///     {
        ///         "name": "api:isConfigured",
        ///         "value": "true",
        ///         "description": "Flag indicating if the API is configured",
        ///         "lastModified": "2024-03-19T10:30:00Z"
        ///     }
        /// </remarks>
        /// <param name="setting">The setting to update</param>
        /// <returns>The updated setting</returns>
        /// <response code="200">Returns the updated setting</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not authorized</response>
        /// <response code="404">If the setting was not found</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPut]
        [ProducesResponseType(typeof(SettingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSettings([FromBody] SettingDto setting)
        {
            try
            {
                if (setting == null)
                {
                    _logger.LogWarning("Invalid request: setting is null");
                    return BadRequest("Setting data is required");
                }

                _logger.LogDebug("Updating setting {SettingName}", setting.Name);
                var updatedSetting = await _settingService.UpdateSettingAsync(setting);
                
                if (updatedSetting == null)
                {
                    _logger.LogWarning("Setting {SettingName} not found", setting.Name);
                    return NotFound($"Setting '{setting.Name}' not found");
                }

                _logger.LogInformation("Successfully updated setting {SettingName}", setting.Name);
                return Ok(updatedSetting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating setting {SettingName}", setting?.Name);
                return StatusCode(500, "An error occurred while updating the setting");
            }
        }

        /// <summary>
        /// Create a new application setting
        /// </summary>
        /// <remarks>   
        /// This endpoint is used to create a new application setting. It requires authentication.
        /// 
        /// Sample request:
        ///     POST /api/v1/settings
        ///     {
        ///         "name": "api:newSetting",
        ///         "value": "value",
        ///         "description": "Description of the new setting"
        ///     }
        /// 
        /// Sample response:
        ///     {
        ///         "name": "api:newSetting",
        ///         "value": "value",
        ///         "description": "Description of the new setting",
        ///         "lastModified": "2024-03-19T10:30:00Z"
        ///     }
        /// </remarks>
        /// <param name="setting">The setting to create</param>
        /// <returns>The newly created setting</returns>
        /// <response code="201">Returns the created setting</response>
        /// <response code="400">If a setting with the same name already exists or if the request data is invalid</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not authorized</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(SettingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateSetting([FromBody] SettingDto setting)
        {
            try
            {
                if (setting == null)
                {
                    _logger.LogWarning("Invalid request: setting is null");
                    return BadRequest("Setting data is required");
                }

                _logger.LogDebug("Creating new setting {SettingName}", setting.Name);
                var createdSetting = await _settingService.CreateSettingAsync(setting);
                _logger.LogInformation("Successfully created setting {SettingName}", setting.Name);
                
                return CreatedAtAction(
                    nameof(GetSettings),
                    new { name = createdSetting.Name },
                    createdSetting
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating setting {SettingName}", setting?.Name);
                return StatusCode(500, "An error occurred while creating the setting");
            }
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
        [ProducesResponseType(typeof(ApiConfigurationDto), StatusCodes.Status200OK)]
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
                    Scheme = await _settingService.GetSettingValueIfExistsAsync("api:scheme", "https", "The scheme of the URL to target API"),
                    Host = await _settingService.GetSettingValueIfExistsAsync("api:host", "localhost", "The IP/DNS of the URL to target API"),
                    Port = await _settingService.GetSettingValueIfExistsAsync("api:port", 5001, "The port of the URL to target API"),
                    AllowedHosts = await _settingService.GetSettingValueIfExistsAsync("api:allowedHosts", "*", "Semicolon-separated list of allowed host names"),
                    AllowedOrigins = await _settingService.GetSettingValueIfExistsAsync("api:allowedOrigins", "*", "Semicolon-separated list of allowed CORS origins"),
                    AllowedMethods = await _settingService.GetSettingValueIfExistsAsync("api:allowedMethods", "GET,POST,DELETE,OPTIONS", "Semicolon-separated list of allowed HTTP methods for CORS"),
                    AllowedHeaders = await _settingService.GetSettingValueIfExistsAsync("api:allowedHeaders", "X-Request-Token,Accept,Content-Type,Authorization", "Semicolon-separated list of allowed HTTP headers for CORS"),
                    ResetPasswordTokenValidity = await _settingService.GetSettingValueIfExistsAsync("api:email:ResetPasswordTokenValidityLifeSpanMinutes", 15, "Validity period in minutes for password reset tokens"),
                    EmailConfirmationTokenValidity = await _settingService.GetSettingValueIfExistsAsync("api:email:confirmationTokenValidityLifeSpanDays", 2, "Validity period in minutes for email confirmation tokens"),
                    RequireDigit = await _settingService.GetSettingValueIfExistsAsync("api:password:requireDigit", true, "Whether passwords must contain at least one digit"),
                    RequireLowercase = await _settingService.GetSettingValueIfExistsAsync("api:password:requireLowercase", true, "Whether passwords must contain at least one lowercase"),
                    RequireNonAlphanumeric = await _settingService.GetSettingValueIfExistsAsync("api:password:requireNonAlphanumeric", true,"Whether passwords must contain at least one non-alphanumeric character"),
                    RequireUppercase = await _settingService.GetSettingValueIfExistsAsync("api:password:requireUppercase", true, "Whether passwords must contain at least one uppercase letter"),
                    RequiredLength = await _settingService.GetSettingValueIfExistsAsync("api:password:requiredLength", 12, "Minimum required length for passwords"),
                    RequiredUniqueChars = await _settingService.GetSettingValueIfExistsAsync("api:password:requiredUniqueChars", 1, "Minimum number of unique characters required in passwords"),
                    SmtpHost = await _settingService.GetSettingValueIfExistsAsync("smtp:host", "", "SMTP server hostname or IP address for sending emails"),
                    SmtpPort = await _settingService.GetSettingValueIfExistsAsync("smtp:port", 587, "SMTP server port number"),
                    SmtpUsername = await _settingService.GetSettingValueIfExistsAsync("smtp:username", "", "Username for SMTP authentication"),
                    SmtpPassword = await _settingService.GetSettingValueIfExistsAsync("smtp:password", "", "Password for SMTP authentication"),
                    SmtpUseSSL = await _settingService.GetSettingValueIfExistsAsync("smtp:useSSL", true, "Whether to use SSL/TLS for SMTP connections"),
                    SmtpSenderEmail = await _settingService.GetSettingValueIfExistsAsync("smtp:senderEmail", "", "Email address to use as the sender address"),
                    SmtpSenderName = await _settingService.GetSettingValueIfExistsAsync("smtp:senderName", "", "Display name to use for the sender"),
                    SmtpRequireAuth = await _settingService.GetSettingValueIfExistsAsync("smtp:requiresAuth", true, "Whether SMTP authentication is required"),
                    RedisEnabled = await _settingService.GetSettingValueIfExistsAsync("redis:enabled", false, "Whether Redis caching is enabled"),
                    RedisHost = await _settingService.GetSettingValueIfExistsAsync("redis:host", "localhost", "Redis server hostname or IP address"),
                    RedisPort = await _settingService.GetSettingValueIfExistsAsync("redis:port", 6379, "Redis server port number"),
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
        /// The operation is idempotent - multiple identical requests will have the same effect as a single request.
        /// 
        /// Sample request:
        ///     PUT /api/v1/settings/api-configuration
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
        ///         "requiredUniqueChars": 1,
        ///         "smtpHost": "smtp.example.com",
        ///         "smtpPort": 587,
        ///         "smtpUsername": "user@example.com",
        ///         "smtpPassword": "password",
        ///         "smtpUseSSL": true,
        ///         "smtpSenderEmail": "noreply@example.com",
        ///         "smtpSenderName": "My Application",
        ///         "smtpRequireAuth": true,
        ///         "redisEnabled": false,
        ///         "redisHost": "localhost",
        ///         "redisPort": 6379
        ///     }
        /// </remarks>
        /// <param name="config">The API configuration to update</param>
        /// <returns>No content</returns>
        /// <response code="204">The configuration was updated successfully</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="403">If the user is not an Admin</response>
        /// <response code="500">If there was an error updating the configuration</response>
        [HttpPut("api-configuration")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
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
                    { "smtp:host", config.SmtpHost },
                    { "smtp:port", config.SmtpPort.ToString() },
                    { "smtp:username", config.SmtpUsername },
                    { "smtp:password", config.SmtpPassword },
                    { "smtp:useSSL", config.SmtpUseSSL.ToString() },
                    { "smtp:senderEmail", config.SmtpSenderEmail },
                    { "smtp:senderName", config.SmtpSenderName },
                    { "smtp:requiresAuth", config.SmtpRequireAuth.ToString() },
                    { "api:redis:enabled", config.RedisEnabled.ToString() },
                    { "api:redis:host", config.RedisHost },
                    { "api:redis:port", config.RedisPort.ToString() },
                };

                await _settingService.UpdateSettings(settings);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API configuration");
                return StatusCode(500, "Error updating API configuration");
            }
        }
    }
}