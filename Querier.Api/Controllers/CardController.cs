using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing dashboard cards
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Creating and managing cards
    /// - Handling card layouts
    /// - Managing card content
    /// - Card visualization settings
    /// </remarks>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class CardController : ControllerBase
    {
        private readonly ICardService _service;

        public CardController(ICardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets a card by its ID
        /// </summary>
        /// <param name="id">The ID of the card to retrieve</param>
        /// <returns>The requested card</returns>
        /// <response code="200">Returns the requested card</response>
        /// <response code="404">If the card is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CardDto>> GetById(int id)
        {
            var card = await _service.GetByIdAsync(id);
            if (card == null) return NotFound();
            return Ok(card);
        }

        /// <summary>
        /// Gets all cards for a specific row
        /// </summary>
        /// <param name="rowId">The ID of the row</param>
        /// <returns>List of cards in the row</returns>
        /// <response code="200">Returns the list of cards</response>
        [HttpGet("row/{rowId}")]
        [ProducesResponseType(typeof(IEnumerable<CardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CardDto>>> GetByRowId(int rowId)
        {
            var cards = await _service.GetByRowIdAsync(rowId);
            return Ok(cards);
        }

        /// <summary>
        /// Creates a new card in a specific row
        /// </summary>
        /// <param name="rowId">The ID of the row to create the card in</param>
        /// <param name="request">The card data</param>
        /// <returns>The created card</returns>
        /// <response code="201">Returns the newly created card</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost("row/{rowId}")]
        [ProducesResponseType(typeof(CardDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CardDto>> Create(int rowId, CardDto request)
        {
            var card = await _service.CreateAsync(rowId, request);
            return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
        }

        /// <summary>
        /// Updates an existing card
        /// </summary>
        /// <param name="id">The ID of the card to update</param>
        /// <param name="request">The updated card data</param>
        /// <returns>The updated card</returns>
        /// <response code="200">Returns the updated card</response>
        /// <response code="404">If the card is not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CardDto>> Update(int id, CardDto request)
        {
            var card = await _service.UpdateAsync(id, request);
            if (card == null) return NotFound();
            return Ok(card);
        }

        /// <summary>
        /// Deletes a card
        /// </summary>
        /// <param name="id">The ID of the card to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the card was successfully deleted</response>
        /// <response code="404">If the card is not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Reorders cards within a row
        /// </summary>
        /// <param name="rowId">The ID of the row containing the cards</param>
        /// <param name="cardIds">Ordered list of card IDs representing the new order</param>
        /// <returns>Success indicator</returns>
        /// <response code="200">If the reordering was successful</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost("row/{rowId}/reorder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reorder(int rowId, [FromBody] List<int> cardIds)
        {
            var result = await _service.ReorderAsync(rowId, cardIds);
            if (!result) return BadRequest();
            return Ok();
        }
    }
} 