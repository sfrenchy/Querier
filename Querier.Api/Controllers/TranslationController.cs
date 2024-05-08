using Querier.Api.Models;
using Querier.Api.Models.Requests;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly ILogger<TranslationController> _logger;
        private ITranslationService _translationService;

        public TranslationController(ILogger<TranslationController> logger, ITranslationService translationService)
        {
            _logger = logger;
            _translationService = translationService;
        }

        [AllowAnonymous]
        [HttpGet("GetTranslations/{languageCode}")]
        public IActionResult GetTranslations(string languageCode)
        {
           return Ok(_translationService.GetTranslations(languageCode));
        }

        [HttpPost("CreateTranslation")]
        public IActionResult CreateTranslation(CreateOrUpdateTranslationRequest request)
        {
            return Ok(_translationService.CreateTranslation(request));
        }

        [HttpPost("UpdateTranslation")]
        public IActionResult UpdateTranslation(CreateOrUpdateTranslationRequest request)
        {
            return Ok(_translationService.UpdateTranslation(request));
        }
    }
}
