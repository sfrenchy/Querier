using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Controllers;

/// <summary>
/// Controller for accessing public application settings
/// </summary>
/// <remarks>
/// This controller provides endpoints for:
/// - Retrieving publicly accessible settings
/// - Accessing non-sensitive configuration data
/// - Getting application status information
/// </remarks>
[AllowAnonymous]
[Route("api/v1/[controller]")]
[ApiController]
public class PublicSettingsController(
    ISettingService settingService,
    ILogger<PublicSettingsController> logger)
    : ControllerBase
{
    /// <summary>
    /// Checks if the application is configured
    /// </summary>
    /// <returns>Boolean indicating if the application is configured</returns>
    /// <response code="200">Returns the configuration status</response>
    /// <response code="500">If an error occurs while checking the configuration status</response>
    [HttpGet("configured")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<bool>> GetApiIsConfigured()
    {
        try
        {
            logger.LogDebug("Checking application configuration status");
            var result = await settingService.GetApiIsConfiguredAsync();
            logger.LogInformation("Application configuration status checked successfully. Status: {Status}", result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while checking application configuration status");
            return Problem(
                title: "Error checking configuration status",
                detail: "An unexpected error occurred while checking the application configuration status",
                statusCode: 500
            );
        }
    }
}