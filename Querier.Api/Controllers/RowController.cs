using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class RowController : ControllerBase
    {
        private readonly IRowService _service;

        public RowController(IRowService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RowDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RowDto>> GetById(int id)
        {
            var row = await _service.GetByIdAsync(id);
            if (row == null) return NotFound();
            return Ok(row);
        }

        [HttpGet("page/{pageId}")]
        [ProducesResponseType(typeof(IEnumerable<RowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RowDto>>> GetByPageId(int pageId)
        {
            var rows = await _service.GetByPageIdAsync(pageId);
            return Ok(rows);
        }

        [HttpPost("page/{pageId}")]
        [ProducesResponseType(typeof(RowDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RowDto>> Create(int pageId, RowCreateDto request)
        {
            var row = await _service.CreateAsync(pageId, request);
            return CreatedAtAction(nameof(GetById), new { id = row.Id }, row);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(RowDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RowDto>> Update(int id, RowCreateDto request)
        {
            var row = await _service.UpdateAsync(id, request);
            if (row == null) return NotFound();
            return Ok(row);
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

        [HttpPost("page/{pageId}/reorder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reorder(int pageId, [FromBody] List<int> rowIds)
        {
            var result = await _service.ReorderAsync(pageId, rowIds);
            if (!result) return BadRequest();
            return Ok();
        }
    }
} 