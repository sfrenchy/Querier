using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.DTOs.Requests.Setup;
using Querier.Api.Domain.Services;

namespace Querier.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class WizardController : ControllerBase
    {
        private readonly ISettingService _settingService;
        private readonly IWizardService _wizardService;
        private readonly ILogger<WizardController> _logger;

        public WizardController(ISettingService settingService, IWizardService wizardService, ILogger<WizardController> logger)
        {
            _settingService = settingService;
            _wizardService = wizardService;
            _logger = logger;
        }

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
        [HttpPost("setup")]
        [AllowAnonymous]
        public async Task<IActionResult> Setup([FromBody] SetupDto request)
        {
            _logger.LogInformation("Starting setup process...");
            _logger.LogInformation("Request data: {@Request}", request);
            
            var isConfigured = await _settingService.GetIsConfigured();
            if (isConfigured)
            {
                _logger.LogWarning("Setup attempted but application is already configured");
                return BadRequest("Application is already configured");
            }

            _logger.LogInformation("Setting up with admin email: {Email}", request.Admin.Email);
            var result = await _wizardService.SetupAsync(request);
            
            if (!result.Success)
            {
                _logger.LogError("Setup failed with error: {Error}", result.Error);
                _logger.LogError("Request validation state: {@ModelState}", ModelState);
                return UnprocessableEntity(result.Error);
            }

            _logger.LogInformation("Setup completed successfully");
            return Ok();
        }
    }
} 