using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;

[AllowAnonymous]  // Ajout de AllowAnonymous au niveau du contrôleur
[Route("api/v1/settings")]
[ApiController]
public class PublicSettingsController : ControllerBase 
{
    private readonly ISettingService _settingService;

    public PublicSettingsController(ISettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet("configured")]
    public async Task<ActionResult<bool>> GetIsConfigured()
    {
        return await _settingService.GetIsConfigured();
    }
} 