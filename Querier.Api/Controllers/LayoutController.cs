using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services.Menu;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing page layouts
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Retrieving complete page layouts with rows and cards
    /// - Updating entire page layouts
    /// - Deleting page layouts
    /// 
    /// ## Authentication
    /// All endpoints in this controller require authentication.
    /// Use a valid JWT token in the Authorization header:
    /// ```
    /// Authorization: Bearer {your-jwt-token}
    /// ```
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

        public LayoutController(ILayoutService layoutService)
        {
            _layoutService = layoutService;
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
            var layout = await _layoutService.GetLayoutAsync(pageId);
            if (layout == null) return NotFound();
            return Ok(layout);
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
            var updatedLayout = await _layoutService.UpdateLayoutAsync(pageId, layout);
            return Ok(updatedLayout);
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
            var result = await _layoutService.DeleteLayoutAsync(pageId);
            if (!result) return NotFound();
            return NoContent();
        }
    }
} 