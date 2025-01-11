using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Controller for managing application pages
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Managing page content
    /// - Handling page layouts
    /// - Page permissions
    /// - Page customization
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class PageController(IPageService pageService, ILogger<PageController> logger) : ControllerBase
    {
        /// <summary>
        /// Gets all pages in a menu category
        /// </summary>
        /// <param name="categoryId">ID of the category</param>
        /// <returns>List of pages</returns>
        /// <response code="200">Returns the list of pages</response>
        /// <response code="404">If the category is not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<PageDto>>> GetAll([FromQuery] int categoryId)
        {
            logger.LogInformation("Getting all pages for category {CategoryId}", categoryId);
            try
            {
                var pages = await pageService.GetAllAsync();
                var pagesList = pages.ToList();
                logger.LogInformation("Successfully retrieved {Count} pages for category {CategoryId}", pagesList.Count, categoryId);
                return Ok(pagesList);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving pages for category {CategoryId}", categoryId);
                return StatusCode(500, new { message = "An error occurred while retrieving the pages" });
            }
        }

        /// <summary>
        /// Gets a page by its ID
        /// </summary>
        /// <param name="id">ID of the page</param>
        /// <returns>The requested page</returns>
        /// <response code="200">Returns the requested page</response>
        /// <response code="404">If the page is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PageDto>> GetById(int id)
        {
            logger.LogInformation("Getting page {PageId}", id);
            try
            {
                var page = await pageService.GetByIdAsync(id);
                if (page == null)
                {
                    logger.LogWarning("Page {PageId} not found", id);
                    return NotFound(new { message = $"Page {id} not found" });
                }

                logger.LogInformation("Successfully retrieved page {PageId}", id);
                return Ok(page);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving page {PageId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the page" });
            }
        }

        /// <summary>
        /// Creates a new page
        /// </summary>
        /// <param name="request">The page data to create</param>
        /// <returns>The created page</returns>
        /// <response code="201">Returns the newly created page</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(PageDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PageDto>> Create(PageCreateDto request)
        {
            if (request == null)
            {
                logger.LogWarning("Attempted to create page with null data");
                return BadRequest(new { message = "Page data cannot be null" });
            }

            logger.LogInformation("Creating new page");
            try
            {
                var result = await pageService.CreateAsync(request);
                logger.LogInformation("Successfully created page {PageId}", result.Id);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating page");
                return StatusCode(500, new { message = "An error occurred while creating the page" });
            }
        }

        /// <summary>
        /// Updates an existing page
        /// </summary>
        /// <param name="id">ID of the page to update</param>
        /// <param name="request">The updated page data</param>
        /// <returns>The updated page</returns>
        /// <response code="200">Returns the updated page</response>
        /// <response code="404">If the page is not found</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PageDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PageDto>> Update(int id, PageUpdateDto request)
        {
            if (request == null)
            {
                logger.LogWarning("Attempted to update page {PageId} with null data", id);
                return BadRequest(new { message = "Page data cannot be null" });
            }

            logger.LogInformation("Updating page {PageId}", id);
            try
            {
                var result = await pageService.UpdateAsync(id, request);
                if (result == null)
                {
                    logger.LogWarning("Page {PageId} not found for update", id);
                    return NotFound(new { message = $"Page {id} not found" });
                }

                logger.LogInformation("Successfully updated page {PageId}", id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating page {PageId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the page" });
            }
        }

        /// <summary>
        /// Deletes a page
        /// </summary>
        /// <param name="id">ID of the page to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the page was successfully deleted</response>
        /// <response code="404">If the page is not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> Delete(int id)
        {
            logger.LogInformation("Deleting page {PageId}", id);
            try
            {
                var result = await pageService.DeleteAsync(id);
                if (!result)
                {
                    logger.LogWarning("Page {PageId} not found for deletion", id);
                    return NotFound(new { message = $"Page {id} not found" });
                }

                logger.LogInformation("Successfully deleted page {PageId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting page {PageId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the page" });
            }
        }
    }
} 