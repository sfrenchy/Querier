using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Services;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing setup wizard functionality
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Checking wizard completion status
    /// - Managing setup wizard steps
    /// - Handling initial application configuration
    /// </remarks>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class WizardController(
        ISettingService settingService,
        IWizardService wizardService,
        ILogger<WizardController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Initial setup of the application
        /// </summary>
        /// <remarks>
        /// This endpoint can only be used while the application is not configured.
        /// After successful setup, it becomes unavailable.
        /// </remarks>
        /// <param name="request">Setup configuration data</param>
        /// <response code="200">Setup completed successfully</response>
        /// <response code="400">If the application is already configured</response>
        /// <response code="422">If the provided data is invalid</response>
        /// <response code="500">If an unexpected error occurs during setup</response>
        [HttpPost("setup")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(422)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Setup([FromBody] SetupDto request)
        {
            try
            {
                logger.LogInformation("Starting setup process...");

                if (request == null)
                {
                    logger.LogWarning("Setup request is null");
                    return BadRequest("Setup configuration data is required");
                }

                if (string.IsNullOrWhiteSpace(request.Admin?.Email))
                {
                    logger.LogWarning("Admin email is missing in setup request");
                    return BadRequest("Admin email is required");
                }

                logger.LogDebug("Validating setup request: {@Request}", new { request.Admin.Email, request.Smtp.Host });
                
                var isConfigured = await settingService.GetApiIsConfiguredAsync();
                if (isConfigured)
                {
                    logger.LogWarning("Setup attempted but application is already configured");
                    return BadRequest("Application is already configured");
                }

                logger.LogInformation("Setting up application with admin email: {Email}", request.Admin.Email);
                var result = await wizardService.SetupAsync(request);
                
                if (!result.Success)
                {
                    logger.LogError("Setup failed with error: {Error}", result.Error);
                    return UnprocessableEntity(new
                    {
                        error = result.Error,
                        validationState = ModelState
                    });
                }

                logger.LogInformation("Setup completed successfully for admin: {Email}", request.Admin.Email);
                return Ok(new { message = "Application setup completed successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during application setup");
                return StatusCode(500, "An unexpected error occurred during application setup");
            }
        }
    }
} 