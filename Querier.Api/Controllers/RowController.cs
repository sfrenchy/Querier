using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class RowController(
        IRowService service,
        ILogger<RowController> logger) : ControllerBase
    {
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
            try
            {
                logger.LogDebug("Retrieving row with ID: {RowId}", id);
                var row = await service.GetByIdAsync(id);
                
                if (row == null)
                {
                    logger.LogWarning("Row not found with ID: {RowId}", id);
                    return NotFound(new { message = $"Row with ID {id} not found" });
                }

                logger.LogInformation("Successfully retrieved row: {RowId}", id);
                return Ok(row);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving row: {RowId}", id);
                return Problem(
                    title: "Error retrieving row",
                    detail: "An unexpected error occurred while retrieving the row",
                    statusCode: 500
                );
            }
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
            try
            {
                logger.LogDebug("Retrieving rows for page: {PageId}", pageId);
                var rows = await service.GetByPageIdAsync(pageId);
                logger.LogInformation("Successfully retrieved rows for page {PageId}. Count: {RowCount}", pageId, rows);
                return Ok(rows);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving rows for page: {PageId}", pageId);
                return Problem(
                    title: "Error retrieving rows",
                    detail: "An unexpected error occurred while retrieving the rows for the page",
                    statusCode: 500
                );
            }
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
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid row creation request for page {PageId}. Errors: {Errors}", 
                        pageId, string.Join(", ", ModelState.Values));
                    return BadRequest(ModelState);
                }

                logger.LogDebug("Creating new row in page: {PageId}", pageId);
                var row = await service.CreateAsync(pageId, request);
                logger.LogInformation("Successfully created row {RowId} in page {PageId}", row.Id, pageId);
                
                return CreatedAtAction(nameof(GetById), new { id = row.Id }, row);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while creating row in page: {PageId}", pageId);
                return Problem(
                    title: "Error creating row",
                    detail: "An unexpected error occurred while creating the row",
                    statusCode: 500
                );
            }
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
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid row update request for ID {RowId}. Errors: {Errors}", 
                        id, string.Join(", ", ModelState.Values));
                    return BadRequest(ModelState);
                }

                logger.LogDebug("Updating row: {RowId}", id);
                var row = await service.UpdateAsync(id, request);
                
                if (row == null)
                {
                    logger.LogWarning("Row not found for update: {RowId}", id);
                    return NotFound(new { message = $"Row with ID {id} not found" });
                }

                logger.LogInformation("Successfully updated row: {RowId}", id);
                return Ok(row);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating row: {RowId}", id);
                return Problem(
                    title: "Error updating row",
                    detail: "An unexpected error occurred while updating the row",
                    statusCode: 500
                );
            }
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
            try
            {
                logger.LogDebug("Attempting to delete row: {RowId}", id);
                var result = await service.DeleteAsync(id);
                
                if (!result)
                {
                    logger.LogWarning("Row not found for deletion: {RowId}", id);
                    return NotFound(new { message = $"Row with ID {id} not found" });
                }

                logger.LogInformation("Successfully deleted row: {RowId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while deleting row: {RowId}", id);
                return Problem(
                    title: "Error deleting row",
                    detail: "An unexpected error occurred while deleting the row",
                    statusCode: 500
                );
            }
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
            try
            {
                if (rowIds == null || rowIds.Count == 0)
                {
                    logger.LogWarning("Invalid reorder request for page {PageId}: Empty or null row IDs", pageId);
                    return BadRequest(new { message = "Row IDs list cannot be empty" });
                }

                logger.LogDebug("Reordering {Count} rows in page {PageId}", rowIds.Count, pageId);
                var result = await service.ReorderAsync(pageId, rowIds);
                
                if (!result)
                {
                    logger.LogWarning("Failed to reorder rows in page {PageId}", pageId);
                    return BadRequest(new { message = "Failed to reorder rows" });
                }

                logger.LogInformation("Successfully reordered rows in page {PageId}", pageId);
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while reordering rows in page: {PageId}", pageId);
                return Problem(
                    title: "Error reordering rows",
                    detail: "An unexpected error occurred while reordering the rows",
                    statusCode: 500
                );
            }
        }
    }
} 