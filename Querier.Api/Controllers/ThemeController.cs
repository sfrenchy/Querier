using Querier.Api.Models.Requests;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ThemeController : ControllerBase
    {
        private readonly ILogger<ThemeController> _logger;
        private IThemeService _themeService;

        public ThemeController(ILogger<ThemeController> logger, IThemeService themeService)
        {
            _logger = logger;
            _themeService = themeService;
        }

        [HttpGet("GetUserThemeList")]
        public IActionResult GetUserThemeList()
        {
            var userId = this.User.Claims.FirstOrDefault(c => c.Type == "Id").Value;
            return Ok(_themeService.GetUserThemeList(userId));
        }

        [HttpGet("GetThemeDefinitionByThemeId/{ThemeId}")]
        public IActionResult GetThemeDefinitionByThemeId(int ThemeId)
        {
            return Ok(_themeService.GetThemeDefinition(ThemeId));
        }

        [HttpGet("GetThemeDefinition/{Label}")]
        public IActionResult GetThemeDefinition(string Label)
        {
            var userId = this.User.Claims.FirstOrDefault(c => c.Type == "Id").Value;
            var currentThemeId = _themeService.GetThemeId(Label, userId); 

            return Ok(_themeService.GetThemeDefinition(currentThemeId));
        }

        [HttpGet("CreateDefaultTheme/{UserId}")]
        public IActionResult CreateDefaultTheme(string UserId)
        {
            return Ok(_themeService.CreateDefaultTheme(UserId));
        }

        [HttpPut("UpdateThemeVariableValues")]
        public IActionResult UpdateThemeVariableValues([FromBody] UpdateThemeRequest TargetTheme) 
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "Id").Value; 
            var currentThemeId = _themeService.GetThemeId(TargetTheme.Label, userId);

            return Ok(_themeService.UpdateThemeVariableValues(currentThemeId, TargetTheme));
        }
    }
}
