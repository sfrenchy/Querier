using Querier.Api.Models.Responses.Role;
using Querier.Api.Services.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Querier.Api.Models.Requests.Role;

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
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _svc;

        public RoleController(IRoleService svc)
        {
            _svc = svc;
        }

        /// <summary>
        /// Retrieves all roles in the system
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/role/getall
        /// </remarks>
        /// <returns>List of all roles</returns>
        /// <response code="200">Returns the list of roles</response>
        [HttpGet("GetAll")]
        [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAsync()
        {
            return Ok(await _svc.GetAll());
        }

        /// <summary>
        /// Creates a new role
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/role/addrole
        ///     {
        ///         "name": "Administrator",
        ///         "description": "Full system access"
        ///     }
        /// </remarks>
        /// <param name="role">The role details to create</param>
        /// <returns>Success indicator</returns>
        /// <response code="200">Role was successfully created</response>
        /// <response code="400">If the request data is invalid</response>
        [HttpPost("AddRole")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddRoleAsync(RoleRequest role)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            return Ok(await _svc.Add(role));
        }

        /// <summary>
        /// Updates an existing role
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/role/updaterole
        ///     {
        ///         "id": "1",
        ///         "name": "Modified Role",
        ///         "description": "Updated description"
        ///     }
        /// </remarks>
        /// <param name="role">The updated role information</param>
        /// <returns>Success indicator</returns>
        [HttpPost("UpdateRole")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateRoleAsync(RoleRequest role)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            return Ok(await _svc.Edit(role));
        }

        /// <summary>
        /// Deletes a role by its ID
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     DELETE /api/v1/role/deleterole/1
        /// </remarks>
        /// <param name="id">The ID of the role to delete</param>
        /// <returns>Success indicator</returns>
        [HttpDelete("DeleteRole/{id}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteRoleAsync(string id)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            return Ok(await _svc.Delete(id));
        }

        /// <summary>
        /// Retrieves all roles assigned to a specific user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/role/getrolesforuser/123
        /// </remarks>
        /// <param name="idUser">The user's ID</param>
        /// <returns>List of roles assigned to the user</returns>
        [HttpGet("GetRolesForUser/{idUser}")]
        [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRolesForUser(string idUser)
        {
            return Ok(await _svc.GetRolesForUser(idUser));
        }

        /// <summary>
        /// Retrieves roles for the currently authenticated user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/role/getcurrentuserroles
        /// </remarks>
        /// <returns>List of roles for the current user</returns>
        [HttpGet("GetCurrentUserRoles")]
        [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentUserRole()
        {
            var userId = this.User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            return Ok(await _svc.GetRolesForUser(userId));
        }
    }
}
