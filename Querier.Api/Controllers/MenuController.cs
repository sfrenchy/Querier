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
    /// Controller for managing menu categories
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Creating, reading, updating and deleting menu categories
    /// - Managing menu category translations
    /// 
    /// ## Authentication
    /// All endpoints in this controller require authentication.
    /// Use a valid JWT token in the Authorization header:
    /// ```
    /// Authorization: Bearer {your-jwt-token}
    /// ```
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _service;
        private readonly ILogger<MenuController> _logger;

        public MenuController(IMenuService service, ILogger<MenuController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Gets all menu categories
        /// </summary>
        /// <returns>List of menu categories with their translations</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MenuDto>>> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        /// <summary>
        /// Gets a menu category by ID
        /// </summary>
        /// <param name="id">The ID of the menu category</param>
        /// <returns>The menu category with its translations</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MenuDto>> GetById(int id)
        {
            var category = await _service.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        /// <summary>
        /// Creates a new menu category
        /// </summary>
        /// <param name="request">The menu category data</param>
        /// <returns>The created menu category</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MenuDto>> Create([FromBody] MenuCreateDto request)
        {
            var category = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        /// <summary>
        /// Updates an existing menu category
        /// </summary>
        /// <param name="id">The ID of the menu category to update</param>
        /// <param name="request">The updated menu category data</param>
        /// <returns>The updated menu category</returns>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MenuDto>> Update(int id, [FromBody] MenuCreateDto request)
        {
            var category = await _service.UpdateAsync(id, request);
            if (category == null)
                return NotFound();

            return Ok(category);
        }

        /// <summary>
        /// Deletes a menu category
        /// </summary>
        /// <param name="id">The ID of the menu category to delete</param>
        /// <returns>Success indicator</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
} 