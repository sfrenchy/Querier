using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing SMTP email configuration
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Configuring SMTP server settings
    /// - Testing email connectivity
    /// - Managing email server configuration
    /// - Validating SMTP settings
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class SmtpController(
        ILogger<SmtpController> logger,
        IEmailSendingService emailService)
        : ControllerBase
    {
        /// <summary>
        /// Test SMTP configuration
        /// </summary>
        /// <remarks>
        /// This endpoint tests if the SMTP configuration is valid by attempting to connect to the SMTP server.
        /// This endpoint is accessible without authentication only during initial setup.
        /// </remarks>
        /// <response code="200">If the connection test is successful</response>
        /// <response code="400">If the connection test fails</response>
        /// <response code="403">If the application is already configured</response>
        [HttpPost("test")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> TestConfiguration([FromBody] SmtpTestDto request)
        {
            try
            {
                logger.LogDebug("Testing SMTP configuration");

                if (request == null)
                {
                    logger.LogWarning("SMTP test request is null");
                    return BadRequest(new { error = "SMTP configuration data is required" });
                }

                var isConfigured = await emailService.IsConfigured();
                if (isConfigured)
                {
                    logger.LogWarning("Attempt to test SMTP configuration when application is already configured");
                    return StatusCode((int)HttpStatusCode.Forbidden, new { error = "Access denied. Application is already configured." });
                }

                logger.LogInformation("Testing SMTP configuration with host: {Host}, port: {Port}, username: {Username}", 
                    request.Host, request.Port, request.Username);
                
                await emailService.TestSmtpConfiguration(request);
                
                logger.LogInformation("SMTP configuration test completed successfully");
                return Ok(new { message = "SMTP configuration test successful" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SMTP configuration test failed");
                return BadRequest(new { 
                    error = "SMTP test failed",
                    details = ex.Message,
                    recommendation = "Please verify your SMTP settings and try again"
                });
            }
        }
    }
} 