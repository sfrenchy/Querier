using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Querier.Api.Application.DTOs.Menu.Requests;
using Querier.Api.Application.Interfaces.Services.Menu;

namespace Querier.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class DynamicRowController : ControllerBase
    {
        private readonly IDynamicRowService _service;

        public DynamicRowController(IDynamicRowService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DynamicRowResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DynamicRowResponse>> GetById(int id)
        {
            var row = await _service.GetByIdAsync(id);
            if (row == null) return NotFound();
            return Ok(row);
        }

        [HttpGet("page/{pageId}")]
        [ProducesResponseType(typeof(IEnumerable<DynamicRowResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DynamicRowResponse>>> GetByPageId(int pageId)
        {
            var rows = await _service.GetByPageIdAsync(pageId);
            return Ok(rows);
        }

        [HttpPost("page/{pageId}")]
        [ProducesResponseType(typeof(DynamicRowResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DynamicRowResponse>> Create(int pageId, CreateDynamicRowRequest request)
        {
            var row = await _service.CreateAsync(pageId, request);
            return CreatedAtAction(nameof(GetById), new { id = row.Id }, row);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DynamicRowResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DynamicRowResponse>> Update(int id, CreateDynamicRowRequest request)
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