using Querier.Api.Services.Repositories.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationSpecificProperties : ControllerBase
    {
        private readonly ILogger _logger;
        public ApplicationSpecificProperties(ILogger<ApplicationSpecificProperties> logger)
        {
            _logger = logger;
        }
        
        [HttpGet]
        [Route("GetFeatures")]
        public IActionResult GetFeatures()
        {
            _logger.LogInformation("Get application features");
            return Ok(Features.EnabledFeatures);
        }

        [HttpGet]
        [Route("GetApplicationName")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult GetApplicationName()
        {
            _logger.LogInformation("Get application name");
            return new JsonResult(Features.ApplicationName);
        }

        [HttpGet]
        [Route("GetApplicationIcon")]
        public IActionResult GetApplicationIcon()
        {
            _logger.LogInformation("Get application Icon");
            return Ok(Features.ApplicationIcon);
        }

        [HttpGet]
        [Route("GetApplicationBackgroundLogin")]
        public IActionResult GetApplicationBackgroundLogin()
        {
            _logger.LogInformation("Get application background login ");
            return Ok(Features.ApplicationBackgroundLogin);
        }
    

        [HttpGet]
        [Route("GetApplicationDefaultTheme")]
        public IActionResult GetApplicationDefaultTheme()
        {
            _logger.LogInformation("Get application default theme");
            return Ok(Features.ApplicationDefaultTheme);
        }

        [HttpGet]
        [Route("GetApplicationRightPanelPackageName")]
        public IActionResult GetApplicationRightPanelPackageName()
        {
            _logger.LogInformation("Get application right panel package name");
            return new JsonResult(Features.ApplicationRightPanelPackageName);
        }

        [HttpGet]
        [Route("GetApplicationUserProperties")]
        public IActionResult GetApplicationUserProperties()
        {
            _logger.LogInformation("Get application user properties");
            return new JsonResult(Features.ApplicationUserProperties);
        }
    }
}