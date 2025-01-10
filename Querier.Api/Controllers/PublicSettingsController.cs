using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Services;

/// <summary>
/// Controller for accessing public application settings
/// </summary>
/// <remarks>
/// This controller provides endpoints for:
/// - Retrieving publicly accessible settings
/// - Accessing non-sensitive configuration data
/// - Getting application status information
/// </remarks>
[AllowAnonymous]  // Ajout de AllowAnonymous au niveau du contrôleur
[Route("api/v1/[controller]")]
[ApiController]
public class PublicSettingsController : ControllerBase 
{
    private readonly ISettingService _settingService;

    public PublicSettingsController(ISettingService settingService)
    {
        _settingService = settingService;
    }

    /// <summary>
    /// Checks if the application is configured
    /// </summary>
    /// <returns>Boolean indicating if the application is configured</returns>
    /// <response code="200">Returns the configuration status</response>
    [HttpGet("configured")]
    public async Task<ActionResult<bool>> GetIsConfigured()
    {
        return await _settingService.GetIsConfigured();
    }
} 