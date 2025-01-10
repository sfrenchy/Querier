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
    /// Controller for managing data rows and records
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Managing data rows
    /// - Handling row operations
    /// - Processing row data
    /// - Row-level security
    /// </remarks>
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

        /// <summary>
        /// Gets a row by its ID
        /// </summary>
        /// <param name="id">The ID of the row to retrieve</param>
        /// <returns>The requested row</returns>
        /// <response code="200">Returns the requested row</response>
        /// <response code="404">If the row is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RowDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RowDto>> GetById(int id)
        {
            var row = await _service.GetByIdAsync(id);
            if (row == null) return NotFound();
            return Ok(row);
        }

        /// <summary>
        /// Gets all rows for a specific page
        /// </summary>
        /// <param name="pageId">The ID of the page</param>
        /// <returns>List of rows in the page</returns>
        /// <response code="200">Returns the list of rows</response>
        [HttpGet("page/{pageId}")]
        [ProducesResponseType(typeof(IEnumerable<RowDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<RowDto>>> GetByPageId(int pageId)
        {
            var rows = await _service.GetByPageIdAsync(pageId);
            return Ok(rows);
        }

        /// <summary>
        /// Creates a new row in a specific page
        /// </summary>
        /// <param name="pageId">The ID of the page to create the row in</param>
        /// <param name="request">The row data</param>
        /// <returns>The created row</returns>
        /// <response code="201">Returns the newly created row</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost("page/{pageId}")]
        [ProducesResponseType(typeof(RowDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RowDto>> Create(int pageId, RowCreateDto request)
        {
            var row = await _service.CreateAsync(pageId, request);
            return CreatedAtAction(nameof(GetById), new { id = row.Id }, row);
        }

        /// <summary>
        /// Updates an existing row
        /// </summary>
        /// <param name="id">The ID of the row to update</param>
        /// <param name="request">The updated row data</param>
        /// <returns>The updated row</returns>
        /// <response code="200">Returns the updated row</response>
        /// <response code="404">If the row is not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(RowDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RowDto>> Update(int id, RowCreateDto request)
        {
            var row = await _service.UpdateAsync(id, request);
            if (row == null) return NotFound();
            return Ok(row);
        }

        /// <summary>
        /// Deletes a row
        /// </summary>
        /// <param name="id">The ID of the row to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the row was successfully deleted</response>
        /// <response code="404">If the row is not found</response>
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
        /// Reorders rows within a page
        /// </summary>
        /// <param name="pageId">The ID of the page containing the rows</param>
        /// <param name="rowIds">Ordered list of row IDs representing the new order</param>
        /// <returns>Success indicator</returns>
        /// <response code="200">If the reordering was successful</response>
        /// <response code="400">If the request is invalid</response>
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