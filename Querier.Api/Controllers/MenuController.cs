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
    /// Controller for managing application menus
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Managing menu items
    /// - Handling menu structure
    /// - Customizing menu settings
    /// - Menu permissions
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class MenuController(IMenuService service, ILogger<MenuController> logger) : ControllerBase
    {
        /// <summary>
        /// Gets all menu categories
        /// </summary>
        /// <returns>List of menu categories with their translations</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MenuDto>>> GetAll()
        {
            logger.LogInformation("Getting all menu categories");
            try
            {
                var categories = await service.GetAllAsync();
                logger.LogInformation("Successfully retrieved {Count} menu categories", categories.Count);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all menu categories");
                return StatusCode(500, new { message = "An error occurred while retrieving menu categories" });
            }
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
            logger.LogInformation("Getting menu category {CategoryId}", id);
            try
            {
                var category = await service.GetByIdAsync(id);
                if (category == null)
                {
                    logger.LogWarning("Menu category {CategoryId} not found", id);
                    return NotFound(new { message = $"Menu category {id} not found" });
                }

                logger.LogInformation("Successfully retrieved menu category {CategoryId}", id);
                return Ok(category);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving menu category {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the menu category" });
            }
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
            if (request == null)
            {
                logger.LogWarning("Attempted to create menu category with null data");
                return BadRequest(new { message = "Menu category data cannot be null" });
            }

            logger.LogInformation("Creating new menu category");
            try
            {
                var category = await service.CreateAsync(request);
                logger.LogInformation("Successfully created menu category {CategoryId}", category.Id);
                return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating menu category");
                return StatusCode(500, new { message = "An error occurred while creating the menu category" });
            }
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
            if (request == null)
            {
                logger.LogWarning("Attempted to update menu category {CategoryId} with null data", id);
                return BadRequest(new { message = "Menu category data cannot be null" });
            }

            logger.LogInformation("Updating menu category {CategoryId}", id);
            try
            {
                var category = await service.UpdateAsync(id, request);
                if (category == null)
                {
                    logger.LogWarning("Menu category {CategoryId} not found for update", id);
                    return NotFound(new { message = $"Menu category {id} not found" });
                }

                logger.LogInformation("Successfully updated menu category {CategoryId}", id);
                return Ok(category);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating menu category {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the menu category" });
            }
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
            logger.LogInformation("Deleting menu category {CategoryId}", id);
            try
            {
                var result = await service.DeleteAsync(id);
                if (!result)
                {
                    logger.LogWarning("Menu category {CategoryId} not found for deletion", id);
                    return NotFound(new { message = $"Menu category {id} not found" });
                }

                logger.LogInformation("Successfully deleted menu category {CategoryId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting menu category {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the menu category" });
            }
        }
    }
} 