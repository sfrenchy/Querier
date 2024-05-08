using Querier.Api.Models;
using Querier.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Querier.Api.Models.Responses;
using Querier.Api.Models.Cards;
using Microsoft.AspNetCore.Authorization;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HtmlEditorController : ControllerBase
    {
        private IHtmlPartialService _htmlPartialService;
        public HtmlEditorController(IHtmlPartialService htmlPartialService)
        {
            _htmlPartialService = htmlPartialService;
        }

        [HttpPost("SaveHtmlPartiel")]
        public async Task<IActionResult> SaveHtmlPartiel([FromBody] AddHtmlContent addRequest) 
        {
            dynamic response = await _htmlPartialService.CreateFilePartialAsync(addRequest.Content, addRequest.LanguageCode, addRequest.CardId);
            return Ok(new { response });
        }
           
        [HttpGet("GetHtmlPartiel/{cardId}/{language}")]
        public async Task<IActionResult> GetHtmlPartiel(int cardId, string language)
        {
            HtmlPartialResponse response = await _htmlPartialService.GetHtmlPart(cardId, language);

            return Ok(new { response });
        }
    }
}
