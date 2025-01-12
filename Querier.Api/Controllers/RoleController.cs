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
    /// Controller for managing roles and role-based access control
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Managing roles (CRUD operations)
    /// - Managing role permissions and actions
    /// - Handling role-page relationships
    /// - User role assignments
    /// 
    /// ## Authentication
    /// All endpoints in this controller require authentication.
    /// Use a valid JWT token in the Authorization header:
    /// ```
    /// Authorization: Bearer {your-jwt-token}
    /// ```
    /// 
    /// ## Common Responses
    /// - 200 OK: Operation completed successfully
    /// - 400 Bad Request: Invalid input data
    /// - 401 Unauthorized: Authentication required
    /// - 403 Forbidden: User lacks required permissions
    /// - 500 Internal Server Error: Unexpected server error
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class RoleController(
        IRoleService roleService,
        IUserService userService,
        ILogger<RoleController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves all roles in the system
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/role
        /// </remarks>
        /// <returns>List of all roles</returns>
        /// <response code="200">Returns the list of roles</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        public IActionResult GetAllAsync()
        {
            try
            {
                logger.LogDebug("Retrieving all roles");
                var roles = roleService.GetAll();
                logger.LogInformation("Successfully retrieved {Count} roles", roles.Count());
                return Ok(roles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving all roles");
                return Problem(
                    title: "Error retrieving roles",
                    detail: "An unexpected error occurred while retrieving the roles",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Retrieves a role by its ID
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/role/1
        /// </remarks>
        /// <param name="id">The unique identifier of the role</param>
        /// <returns>The role details</returns>
        /// <response code="200">Returns the requested role</response>
        /// <response code="404">If the role was not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            try
            {
                logger.LogDebug("Retrieving role with ID: {RoleId}", id);
                var role = await roleService.GetByIdAsync(id);
                
                if (role == null)
                {
                    logger.LogWarning("Role not found with ID: {RoleId}", id);
                    return NotFound(new { message = $"Role with ID {id} not found" });
                }

                logger.LogInformation("Successfully retrieved role: {RoleId}", id);
                return Ok(role);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving role with ID: {RoleId}", id);
                return Problem(
                    title: "Error retrieving role",
                    detail: "An unexpected error occurred while retrieving the role",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Creates a new role
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/role
        ///     {
        ///         "name": "Administrator",
        ///         "description": "Full system access"
        ///     }
        /// </remarks>
        /// <param name="role">The role details to create</param>
        /// <returns>The created role</returns>
        /// <response code="201">Role was successfully created</response>
        /// <response code="400">If the request data is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAsync(RoleCreateDto role)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid role creation request. Validation errors: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                logger.LogDebug("Creating new role with name: {RoleName}", role.Name);
                var createdRole = await roleService.AddAsync(role);
                logger.LogInformation("Role created successfully: {RoleName}", role.Name);
                return CreatedAtAction(nameof(GetByIdAsync), new { id = createdRole.Id }, createdRole);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while creating role: {RoleName}", role?.Name);
                return Problem(
                    title: "Error creating role",
                    detail: "An unexpected error occurred while creating the role",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Updates an existing role
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/v1/role/1
        ///     {
        ///         "id": "1",
        ///         "name": "Modified Role",
        ///         "description": "Updated description"
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the role to update</param>
        /// <param name="role">The updated role information</param>
        /// <returns>Success indicator</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAsync(string id, RoleDto role)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid role update request. Validation errors: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return BadRequest(ModelState);
                }

                logger.LogDebug("Updating role: {RoleId} - {RoleName}", role.Id, role.Name);
                var result = await roleService.UpdateAsync(role);
                if (!result)
                {
                    logger.LogWarning("Role not found for update: {RoleId}", role.Id);
                    return NotFound(new { message = $"Role with ID {role.Id} not found" });
                }

                logger.LogInformation("Role updated successfully: {RoleId} - {RoleName}", role.Id, role.Name);
                return Ok(true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while updating role: {RoleId} - {RoleName}", role?.Id, role?.Name);
                return Problem(
                    title: "Error updating role",
                    detail: "An unexpected error occurred while updating the role",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Deletes a role by its ID
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     DELETE /api/v1/role/1
        /// </remarks>
        /// <param name="id">The ID of the role to delete</param>
        /// <returns>Success indicator</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            try
            {
                logger.LogDebug("Attempting to delete role: {RoleId}", id);
                var result = await roleService.DeleteByIdAsync(id);
                if (!result)
                {
                    logger.LogWarning("Role not found for deletion: {RoleId}", id);
                    return NotFound(new { message = $"Role with ID {id} not found" });
                }

                logger.LogInformation("Role deleted successfully: {RoleId}", id);
                return Ok(true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while deleting role: {RoleId}", id);
                return Problem(
                    title: "Error deleting role",
                    detail: "An unexpected error occurred while deleting the role",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Retrieves all roles assigned to a specific user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/role/user/123
        /// </remarks>
        /// <param name="userId">The user's ID</param>
        /// <returns>List of roles assigned to the user</returns>
        [HttpGet("user/{userId}")]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRolesForUser(string userId)
        {
            try
            {
                logger.LogDebug("Retrieving roles for user: {UserId}", userId);
                var roles = await roleService.GetRolesForUserAsync(userId);
                logger.LogInformation("Successfully retrieved {Count} roles for user: {UserId}", roles.Count(), userId);
                return Ok(roles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving roles for user: {UserId}", userId);
                return Problem(
                    title: "Error retrieving user roles",
                    detail: "An unexpected error occurred while retrieving the user's roles",
                    statusCode: 500
                );
            }
        }

        /// <summary>
        /// Retrieves roles for the currently authenticated user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/role/me
        /// </remarks>
        /// <returns>List of roles for the current user</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUserRoles()
        {
            try
            {
                logger.LogDebug("Retrieving roles for current user");
                var user = await userService.GetCurrentUserAsync(User);
                if (user == null)
                {
                    logger.LogWarning("Current user not found");
                    return NotFound(new { message = "Current user not found" });
                }

                logger.LogInformation("Successfully retrieved roles for current user: {UserEmail}", user.Email);
                return Ok(user.Roles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while retrieving roles for current user");
                return Problem(
                    title: "Error retrieving current user roles",
                    detail: "An unexpected error occurred while retrieving the current user's roles",
                    statusCode: 500
                );
            }
        }
    }
}
