using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Querier.Api.Application.DTOs.Menu.Requests;
using Querier.Api.Application.DTOs.Menu.Responses;
using Querier.Api.Application.Interfaces.Services.Menu;

namespace Querier.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class DynamicCardController : ControllerBase
    {
        private readonly IDynamicCardService _service;

        public DynamicCardController(IDynamicCardService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DynamicCardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DynamicCardResponse>> GetById(int id)
        {
            var card = await _service.GetByIdAsync(id);
            if (card == null) return NotFound();
            return Ok(card);
        }

        [HttpGet("row/{rowId}")]
        [ProducesResponseType(typeof(IEnumerable<DynamicCardResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DynamicCardResponse>>> GetByRowId(int rowId)
        {
            var cards = await _service.GetByRowIdAsync(rowId);
            return Ok(cards);
        }

        [HttpPost("row/{rowId}")]
        [ProducesResponseType(typeof(DynamicCardResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DynamicCardResponse>> Create(int rowId, CreateDynamicCardRequest request)
        {
            var card = await _service.CreateAsync(rowId, request);
            return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DynamicCardResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DynamicCardResponse>> Update(int id, CreateDynamicCardRequest request)
        {
            var card = await _service.UpdateAsync(id, request);
            if (card == null) return NotFound();
            return Ok(card);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }

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