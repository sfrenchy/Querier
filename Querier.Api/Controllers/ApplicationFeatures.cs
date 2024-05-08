using Querier.Api.Services.Repositories.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationFeatures : ControllerBase
    {
        private readonly ILogger _logger;
        public ApplicationFeatures(ILogger<ApplicationFeatures> logger)
        {
            _logger = logger;
        }
        
        [HttpGet]
        [Route("Get")]
        public IActionResult Get()
        {
            _logger.LogInformation("Get application features");
            return Ok(Features.EnabledFeatures);
        }
    }
}