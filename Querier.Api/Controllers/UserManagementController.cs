﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Querier.Api.Application.DTOs.Auth.Email;
using Querier.Api.Application.DTOs.Auth.Password;
using Querier.Api.Application.DTOs.Requests.Auth;
using Querier.Api.Application.DTOs.Requests.User;
using Querier.Api.Application.Interfaces.Services.User;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing user accounts and operations
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - User CRUD operations (Create, Read, Update, Delete)
    /// - Password management
    /// - Email confirmation
    /// - User listing and search
    /// 
    /// ## Authentication
    /// Most endpoints in this controller require authentication except for password reset and email confirmation.
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
    public class UserManagementController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserManagementController> _logger;
        private readonly UserManager<ApiUser> _userManager;

        public UserManagementController(IUserService svc, ILogger<UserManagementController> logger, UserManager<ApiUser> userManager)
        {
            _userService = svc;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Retrieves a user by their ID
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/usermanagement/view/123
        /// </remarks>
        /// <param name="id">The unique identifier of the user</param>
        /// <returns>The user details</returns>
        /// <response code="200">Returns the requested user</response>
        /// <response code="400">If the ID is invalid</response>
        /// <response code="404">If the user was not found</response>
        [HttpGet("View/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ViewAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Request parameter is not valid");

            return Ok(await _userService.View(id));
        }

        /// <summary>
        /// Creates a new user account
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/v1/usermanagement/add
        ///     {
        ///         "email": "user@example.com",
        ///         "username": "johndoe",
        ///         "password": "SecurePassword123!"
        ///     }
        /// </remarks>
        /// <param name="user">The user details for registration</param>
        /// <returns>Success indicator</returns>
        /// <response code="200">User was successfully created</response>
        /// <response code="400">If the request data is invalid</response>
        [HttpPut]
        [Route("Add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddAsync([FromBody] UserRequest user)
        {
            if (!ModelState.IsValid)
                return BadRequest("Request body is not valid");

            if (await _userService.Add(user))
                return Ok(true);
            else
                return StatusCode(500);
        }

        /// <summary>
        /// Updates an existing user's information
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/v1/usermanagement/update
        ///     {
        ///         "id": "123",
        ///         "email": "updated@example.com",
        ///         "username": "janedoe"
        ///     }
        /// </remarks>
        /// <param name="user">The updated user information</param>
        /// <returns>Success indicator</returns>
        [HttpPut]
        [Route("Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateAsync([FromBody] UserRequest user)
        {
            if (!ModelState.IsValid)
                return BadRequest("Request body is not valid");

            if (await _userService.Update(user))
                return Ok(true);
            else
                return StatusCode(500);
        }

        /// <summary>
        /// Deletes a user account
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     DELETE /api/v1/usermanagement/delete/123
        /// </remarks>
        /// <param name="id">The ID of the user to delete</param>
        /// <returns>Success indicator</returns>
        [HttpDelete]
        [Route("Delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            if (!ModelState.IsValid)
                return BadRequest("Request is not valid");

            var res = await _userService.Delete(id);
            if (res)
                return Ok(res);

            return BadRequest($"Cannot delete user with id = {id}");
        }

        /// <summary>
        /// Retrieves all users without pagination
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/usermanagement/getall
        /// </remarks>
        /// <returns>Complete list of users</returns>
        [HttpGet]
        [Route("GetAll")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        /// <summary>
        /// Resets a user's password using the reset token
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/usermanagement/resetpassword
        ///     {
        ///         "token": "reset-token",
        ///         "newPassword": "NewSecurePassword123!"
        ///     }
        /// </remarks>
        /// <param name="reset_password_infos">The reset token and new password</param>
        /// <returns>Result of the password reset operation</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("resetPassword")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPassword reset_password_infos)
        {
            var response = await _userService.ResetPassword(reset_password_infos);
            return Ok(response);
        }

        /// <summary>
        /// Confirms a user's email address
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/usermanagement/emailconfirmation/confirmation-token/user@example.com
        /// </remarks>
        /// <param name="token">The email confirmation token</param>
        /// <param name="mail">The email address to confirm</param>
        /// <returns>Result of the email confirmation operation</returns>
        [HttpGet]
        [Route("emailConfirmation/{token}/{mail}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> EmailConfirmation(string token, string mail)
        {            
            var response = await _userService.EmailConfirmation(new EmailConfirmation { Email = mail, Token = token });
            return Ok(response);
        }

        /// <summary>
        /// Retrieves the current user's profile
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/usermanagement/me
        /// </remarks>
        /// <returns>The current user's details</returns>
        /// <response code="200">Returns the current user</response>
        /// <response code="401">If the user is not authenticated</response>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            var user = await _userService.GetCurrentUser(User);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPost("resend-confirmation")]
        [Authorize]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request)
        {
            var result = await _userService.ResendConfirmationEmail(request.UserId);

            if (result)
            {
                return Ok();
            }

            return BadRequest("Failed to send confirmation email");
        }
    }
}

