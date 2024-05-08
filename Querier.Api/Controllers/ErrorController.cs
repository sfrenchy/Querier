using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Controllers
{
    public class ErrorController : ControllerBase
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [HttpGet]
        [HttpDelete]
        [HttpHead]
        [HttpOptions]
        [HttpPatch]
        [HttpPut]
        [Route("/error")]
        public IActionResult Error([FromServices] IWebHostEnvironment webHostEnvironment)
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            if (context != null)
            {
                _logger.LogDebug(context.Error, context.Error.Message);
                return Problem(
                    detail: context.Error.StackTrace,
                    title: context.Error.Message
                );
            }

            return Problem();
        }
    }
}
