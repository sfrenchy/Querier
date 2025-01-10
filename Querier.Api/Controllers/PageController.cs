using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

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
    public class PageController : ControllerBase
    {
        private readonly IPageService _pageService;

        public PageController(IPageService pageService)
        {
            _pageService = pageService;
        }

        /// <summary>
        /// Gets all pages in a menu category
        /// </summary>
        /// <param name="categoryId">ID of the category</param>
        /// <returns>List of pages</returns>
        /// <response code="200">Returns the list of pages</response>
        /// <response code="404">If the category is not found</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PageDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<PageDto>>> GetAll([FromQuery] int categoryId)
        {
            return Ok(await _pageService.GetAllAsync());
        }

        /// <summary>
        /// Gets a page by its ID
        /// </summary>
        /// <param name="id">ID of the page</param>
        /// <returns>The requested page</returns>
        /// <response code="200">Returns the requested page</response>
        /// <response code="404">If the page is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(PageDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<PageDto>> GetById(int id)
        {
            var page = await _pageService.GetByIdAsync(id);
            if (page == null) return NotFound();
            return Ok(page);
        }

        /// <summary>
        /// Creates a new page
        /// </summary>
        /// <param name="request">The page data to create</param>
        /// <returns>The created page</returns>
        /// <response code="201">Returns the newly created page</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(PageDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PageDto>> Create(PageCreateDto request)
        {
            var result = await _pageService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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
        [ProducesResponseType(typeof(PageDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PageDto>> Update(int id, PageUpdateDto request)
        {
            var result = await _pageService.UpdateAsync(id, request);
            if (result == null) return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Deletes a page
        /// </summary>
        /// <param name="id">ID of the page to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the page was successfully deleted</response>
        /// <response code="404">If the page is not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _pageService.DeleteAsync(id);
            if (!result) return NotFound();
            return NoContent();
        }
    }
} 