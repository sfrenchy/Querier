using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Common.Models;

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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class CardController : ControllerBase
    {
        private readonly ICardService service;
        private readonly ILogger<CardController> logger;

        public CardController(ICardService service, ILogger<CardController> logger)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        public async Task<ActionResult<CardDto>> GetById([FromRoute] int id)
        {
            try
            {
                logger.LogInformation("Retrieving card with ID: {Id}", id);
                var card = await service.GetByIdAsync(id);

                if (card == null)
                {
                    logger.LogWarning("Card not found with ID: {Id}", id);
                    return NotFound(new { error = $"Card with ID {id} not found" });
                }

                logger.LogInformation("Successfully retrieved card with ID: {Id}", id);
                return Ok(card);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving card with ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets all cards for a specific row
        /// </summary>
        /// <param name="rowId">The ID of the row</param>
        /// <returns>List of cards in the row</returns>
        /// <response code="200">Returns the list of cards</response>
        [HttpGet("row/{rowId}")]
        [ProducesResponseType(typeof(IEnumerable<CardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CardDto>>> GetByRowId([FromRoute] int rowId)
        {
            try
            {
                logger.LogInformation("Retrieving cards for row ID: {RowId}", rowId);
                var cards = await service.GetByRowIdAsync(rowId);
                logger.LogInformation("Successfully retrieved cards for row ID: {RowId}", rowId);
                return Ok(cards);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving cards for row ID: {RowId}", rowId);
                throw;
            }
        }

        /// <summary>
        /// Gets paged cards for a specific row
        /// </summary>
        /// <param name="rowId">The ID of the row</param>
        /// <param name="parameters">The pagination parameters</param>
        /// <returns>Paged list of cards in the row</returns>
        /// <response code="200">Returns the paged list of cards</response>
        [HttpPost("row/{rowId}/paged")]
        [ProducesResponseType(typeof(DataPagedResult<CardDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<DataPagedResult<CardDto>>> GetByRowIdPaged(
            [FromRoute] int rowId,
            [FromBody] DataRequestParametersDto parameters)
        {
            try
            {
                logger.LogInformation("Retrieving paged cards for row ID: {RowId}, Page: {PageNumber}, Size: {PageSize}", 
                    rowId, parameters.PageNumber, parameters.PageSize);
                
                var result = await service.GetByRowIdPagedAsync(rowId, parameters);
                
                logger.LogInformation("Successfully retrieved paged cards for row ID: {RowId}", rowId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving paged cards for row ID: {RowId}", rowId);
                throw;
            }
        }

        /// <summary>
        /// Creates a new card in a specific row
        /// </summary>
        /// <param name="rowId">The ID of the row to create the card in</param>
        /// <param name="request">The card data</param>
        /// <returns>The created card</returns>
        /// <response code="201">Returns the newly created card</response>
        /// <response code="400">If the request is invalid</response>
        /// <response code="404">If the row is not found</response>
        [HttpPost("row/{rowId}")]
        [ProducesResponseType(typeof(CardDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CardDto>> Create([FromRoute] int rowId, [FromBody] CardDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state for card creation in row ID: {RowId}", rowId);
                    return BadRequest(ModelState);
                }

                logger.LogInformation("Creating new card in row ID: {RowId}", rowId);
                
                try
                {
                    var card = await service.CreateAsync(rowId, request);
                    logger.LogInformation("Successfully created card with ID: {Id} in row: {RowId}", 
                        card.Id, rowId);
                    return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogWarning(ex, "Row not found for card creation. Row ID: {RowId}", rowId);
                    return NotFound(new { error = $"Row with ID {rowId} not found" });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating card in row ID: {RowId}", rowId);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing card
        /// </summary>
        /// <param name="id">The ID of the card to update</param>
        /// <param name="request">The updated card data</param>
        /// <returns>The updated card</returns>
        /// <response code="200">Returns the updated card</response>
        /// <response code="400">If the request is invalid</response>
        /// <response code="404">If the card is not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(CardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CardDto>> Update([FromRoute] int id, [FromBody] CardDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state for updating card ID: {Id}", id);
                    return BadRequest(ModelState);
                }

                logger.LogInformation("Updating card with ID: {Id}", id);
                var card = await service.UpdateAsync(id, request);

                if (card == null)
                {
                    logger.LogWarning("Card not found for update with ID: {Id}", id);
                    return NotFound(new { error = $"Card with ID {id} not found" });
                }

                logger.LogInformation("Successfully updated card with ID: {Id}", id);
                return Ok(card);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating card with ID: {Id}", id);
                throw;
            }
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
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                logger.LogInformation("Deleting card with ID: {Id}", id);
                var result = await service.DeleteAsync(id);

                if (!result)
                {
                    logger.LogWarning("Card not found for deletion with ID: {Id}", id);
                    return NotFound(new { error = $"Card with ID {id} not found" });
                }

                logger.LogInformation("Successfully deleted card with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting card with ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// Reorders cards within a row
        /// </summary>
        /// <param name="rowId">The ID of the row containing the cards</param>
        /// <param name="cardIds">Ordered list of card IDs representing the new order</param>
        /// <returns>Success indicator</returns>
        /// <response code="200">If the reordering was successful</response>
        /// <response code="400">If the request is invalid or some cards were not found</response>
        [HttpPost("row/{rowId}/reorder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reorder([FromRoute] int rowId, [FromBody][Required] List<int> cardIds)
        {
            try
            {
                if (cardIds == null || cardIds.Count == 0)
                {
                    logger.LogWarning("Invalid card IDs list for reordering in row ID: {RowId}", rowId);
                    return BadRequest(new { error = "Card IDs list cannot be empty" });
                }

                logger.LogInformation("Reordering {Count} cards in row ID: {RowId}", cardIds.Count, rowId);
                var result = await service.ReorderAsync(rowId, cardIds);

                if (!result)
                {
                    logger.LogWarning("Some cards were not found during reordering in row ID: {RowId}", rowId);
                    return BadRequest(new { error = "Some cards were not found" });
                }

                logger.LogInformation("Successfully reordered cards in row ID: {RowId}", rowId);
                return Ok(new { message = "Cards reordered successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reordering cards in row ID: {RowId}", rowId);
                throw;
            }
        }
    }
} 