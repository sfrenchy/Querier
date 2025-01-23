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
    /// Controller for managing application layouts
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Managing page layouts
    /// - Handling layout templates
    /// - Customizing layout settings
    /// - Layout persistence
    /// </remarks>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class LayoutController : ControllerBase
    {
        private readonly ILayoutService _layoutService;
        private readonly ILogger<LayoutController> _logger;

        public LayoutController(ILayoutService layoutService, ILogger<LayoutController> logger)
        {
            _layoutService = layoutService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a complete page layout by ID
        /// </summary>
        /// <remarks>
        /// Retrieves the complete layout of a page, including all its rows and cards.
        /// 
        /// Sample request:
        ///     GET /api/v1/layout/123
        /// </remarks>
        /// <param name="pageId">The ID of the page</param>
        /// <returns>The complete layout of the page</returns>
        /// <response code="200">Returns the requested layout</response>
        /// <response code="404">If the page was not found</response>
        [HttpGet("{pageId}")]
        [ProducesResponseType(typeof(LayoutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LayoutDto>> GetLayout(int pageId)
        {
            _logger.LogInformation("Getting layout for page {PageId}", pageId);
            try
            {
                var layout = await _layoutService.GetLayoutAsync(pageId);
                if (layout == null)
                {
                    _logger.LogWarning("Layout not found for page {PageId}", pageId);
                    return NotFound(new { message = $"Layout not found for page {pageId}" });
                }
                _logger.LogInformation("Successfully retrieved layout for page {PageId}", pageId);
                return Ok(layout);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error getting layout for page {PageId}", pageId);
                return StatusCode(500, new { message = "An error occurred while retrieving the layout" });
            }
        }

        /// <summary>
        /// Updates a complete page layout
        /// </summary>
        /// <remarks>
        /// Updates the entire layout of a page, including all its rows and cards.
        /// This is an atomic operation - either all changes are applied, or none are.
        /// 
        /// Sample request:
        ///     PUT /api/v1/layout/123
        ///     {
        ///         "pageId": 123,
        ///         "icon": "dashboard",
        ///         "names": {
        ///             "en": "Dashboard",
        ///             "fr": "Tableau de bord"
        ///         },
        ///         "isVisible": true,
        ///         "roles": ["Admin", "User"],
        ///         "route": "/dashboard",
        ///         "rows": [
        ///             {
        ///                 "order": 1,
        ///                 "alignment": "Start",
        ///                 "crossAlignment": "Start",
        ///                 "spacing": 16.0,
        ///                 "cards": []
        ///             }
        ///         ]
        ///     }
        /// </remarks>
        /// <param name="pageId">The ID of the page to update</param>
        /// <param name="layout">The new layout configuration</param>
        /// <returns>The updated layout</returns>
        /// <response code="200">Returns the updated layout</response>
        /// <response code="404">If the page was not found</response>
        /// <response code="400">If the layout data is invalid</response>
        [HttpPut("{pageId}")]
        [ProducesResponseType(typeof(LayoutDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LayoutDto>> UpdateLayout(int pageId, [FromBody] LayoutDto layout)
        {
            _logger.LogInformation("Updating layout for page {PageId}", pageId);
            
            if (layout == null)
            {
                _logger.LogWarning("Invalid layout data provided for page {PageId}", pageId);
                return BadRequest(new { message = "Layout data cannot be null" });
            }

            if (pageId != layout.PageId)
            {
                _logger.LogWarning("Mismatched page IDs: URL {UrlPageId} vs Body {BodyPageId}", pageId, layout.PageId);
                return BadRequest(new { message = "Page ID in URL does not match the one in request body" });
            }

            try
            {
                var updatedLayout = await _layoutService.UpdateLayoutAsync(pageId, layout);
                if (updatedLayout == null)
                {
                    _logger.LogWarning("Layout not found for update on page {PageId}", pageId);
                    return NotFound(new { message = $"Layout not found for page {pageId}" });
                }
                _logger.LogInformation("Successfully updated layout for page {PageId}", pageId);
                return Ok(updatedLayout);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error updating layout for page {PageId}", pageId);
                return StatusCode(500, new { message = "An error occurred while updating the layout" });
            }
        }

        /// <summary>
        /// Deletes a page layout
        /// </summary>
        /// <remarks>
        /// Deletes a page layout and all its associated rows and cards.
        /// This operation cannot be undone.
        /// 
        /// Sample request:
        ///     DELETE /api/v1/layout/123
        /// </remarks>
        /// <param name="pageId">The ID of the page to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the layout was successfully deleted</response>
        /// <response code="404">If the page was not found</response>
        [HttpDelete("{pageId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteLayout(int pageId)
        {
            _logger.LogInformation("Deleting layout for page {PageId}", pageId);
            try
            {
                var result = await _layoutService.DeleteLayoutAsync(pageId);
                if (!result)
                {
                    _logger.LogWarning("Layout not found for deletion on page {PageId}", pageId);
                    return NotFound(new { message = $"Layout not found for page {pageId}" });
                }
                _logger.LogInformation("Successfully deleted layout for page {PageId}", pageId);
                return NoContent();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error deleting layout for page {PageId}", pageId);
                return StatusCode(500, new { message = "An error occurred while deleting the layout" });
            }
        }
    }
} 