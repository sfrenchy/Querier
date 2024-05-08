using Querier.Api.Models;
using Querier.Api.Models.Cards;
using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Querier.Api.Services.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Querier.Api.Controllers.UI
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UICardController : ControllerBase
    {

        private readonly ILogger<UICardController> _logger;
        private readonly IUICardService _uiCardService;

        /// <summary>
        /// Constructor
        /// </summary>
        public UICardController(ILogger<UICardController> logger, IUICardService uicardService)
        {
            _logger = logger;
            _uiCardService = uicardService;
        }

        /// <summary>
        /// Used to get all card for one row
        /// </summary>
        /// <param name="rowId">The id of the row</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpGet]
        [Route("GetCards/{rowId}")]
        public async Task<IActionResult> GetCardsAsync(int rowId)
        {
            return new OkObjectResult(await _uiCardService.GetCardsAsync(rowId));
        }

        /// <summary>
        /// Used to create a new card
        /// </summary>
        /// <param name="card">The add card request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost]
        [Route("AddCard")]
        public async Task<IActionResult> AddCardAsync([FromBody] AddCardRequest card)
        {
            return new OkObjectResult(await _uiCardService.AddCardAsync(card));
        }

        /// <summary>
        /// Used to update a card
        /// </summary>
        /// <param name="updateCardRequest">The update card request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPut("UpdateCard")]
        public async Task<IActionResult> UpdateCardAsync([FromBody] QPageCard cardUpdated)
        {
            QPageCard card = await _uiCardService.UpdateCardAsync(cardUpdated);

            if (card == null)
                return NotFound("The card was not found!");

            return new OkObjectResult(card);           
        }

        /// <summary>
        /// Used to delete a card
        /// </summary>
        /// <param name="cardId">The id of the card delete</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpDelete("DeleteCard/{cardId}")]
        public async Task<IActionResult> DeleteCardAsync(int cardId)
        {
            QPageCard card = await _uiCardService.DeleteCardAsync(cardId);

            if (card == null)
                return NotFound("The card was not found!");

            return new OkObjectResult(await _uiCardService.GetCardsAsync(card.HAPageRowId));
        }

        /// <summary>
        /// Used to add a new predifined card
        /// </summary>
        /// <param name="model">The predifined card model</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("AddPredefinedCard")]
        public async Task<IActionResult> AddPredefinedCardAsync([FromBody] AddPredefinedCardRequest model)
        {
            return new OkObjectResult(await _uiCardService.AddPredefinedCardAsync(model));
        }

        [HttpGet("GetPredefinedCards")]
        public async Task<IActionResult> GetPredefinedCards()
        {
            return new OkObjectResult(await _uiCardService.GetPredefinedCards());
        }
        /// <summary>
        /// Used to get the content for a card
        /// </summary>
        /// <param name="haPageCardId">The card id</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpGet("CardContent/{haPageCardId}")]
        public async Task<IActionResult> CardContentAsync(int haPageCardId)
        {
            QPageCard card = await _uiCardService.CardContentAsync(haPageCardId);

            if (card == null)
                return NotFound("The card has been not found!");

            return new OkObjectResult(card);
        }

        /// <summary>
        /// Used to saved a predefined configuration for card
        /// </summary>
        /// <param name="model">The request who carry the model to save</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("SaveCardConfiguration")]
        public async Task<IActionResult> SaveCardConfigurationAsync([FromBody] CardDefinedConfigRequest model)
        {           
            return new OkObjectResult(await _uiCardService.SaveCardConfigurationAsync(model));
        }

        /// <summary>
        /// Used to export a configuration for card
        /// </summary>
        /// <param name="model">The request who carry the model to save</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("ExportCardConfiguration")]
        public async Task<IActionResult> ExportCardConfigurationAsync([FromBody] CardDefinedConfigRequest model)
        {            
            return new OkObjectResult(await _uiCardService.ExportCardConfigurationAsync(model));
        }

        /// <summary>
        /// Used to export a configuration for card
        /// </summary>
        /// <param name="model">The request who carry the model to save</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("ImportCardConfiguration")]
        public async Task<IActionResult> ImportCardConfigurationAsync()
        {
            if (Request.Form.Files != null)
            {
                if (Request.Form.Files.Count == 1)
                {
                    string tempPath = Path.GetTempFileName();
                    using (var requestFileStream = Request.Form.Files[0].OpenReadStream())
                    using (var stream = System.IO.File.Create(tempPath))
                    {
                        await requestFileStream.CopyToAsync(stream);
                    }

                    return new OkObjectResult(await _uiCardService.ImportCardConfigurationAsync(new CardImportConfigRequest()
                    {
                        FilePath = tempPath,
                        PageRowId = int.Parse(Request.Form["pageRowId"].ToString())
                    }));
                }
                else
                {
                    throw new FileLoadException("Only one file is allowed when importing entities");
                }
            }

            throw new FileLoadException("No file attached");
        }


        /// <summary>
        /// Used to update a saved card configuration
        /// </summary>
        /// <param name="newConfiguration">The new configuration for the saved configuration</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("UpdateCardConfiguration")]
        public async Task<IActionResult> UpdateCardConfigurationAsync([FromBody] dynamic newConfiguration)
        {
            return new OkObjectResult(await _uiCardService.UpdateCardConfigurationAsync(newConfiguration));
        }

        /// <summary>
        /// Used to get the configuration for a card
        /// </summary>
        /// <param name="cardId">The card id</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpGet("GetCardConfiguration/{cardId}")]
        public async Task<IActionResult> GetCardConfigurationAsync(int cardId)
        {
            QPageCard card = await _uiCardService.GetCardConfigurationAsync(cardId);

            if (card == null)
                return NotFound("The card has been not found!");

            return new OkObjectResult(card.CardConfiguration);
        }

        /// <summary>
        /// Used to get the configuration for a card
        /// </summary>
        /// <param name="cardId">The card id</param>
        /// <param name="cardRowId">The row id</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpGet("CardMaxWidth")]
        public ActionResult CardMaxWidth(int cardId, int cardRowId)
        {
            if (cardRowId == 0)
                return BadRequest($"Card with rowId = 0 not found");
            if (cardId == 0)
                return BadRequest($"Card with Id = 0 not found");

            return Ok(_uiCardService.CardMaxWidth(cardId, cardRowId));
        }

        /// <summary>
        /// Used to update the order of cards
        /// </summary>
        /// <param name="row">the row to update the order of its cards</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPut("UpdateCardOrder")]
        public async Task<IActionResult> UpdateCardOrderAsync([FromBody] QPageRowVM row)
        {
            if (row == null)
                return NotFound("Unable to find the row!");

            List<QPageCard> cards = await _uiCardService.UpdateCardOrder(row);
            return new OkObjectResult(cards);
        }
    }
}