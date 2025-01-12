using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing user accounts and profiles
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - User account management
    /// - Profile updates and settings
    /// - User preferences
    /// - Account status management
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class UserController(
        IUserService userService,
        ILogger<UserController> logger)
        : ControllerBase
    {

        /// <summary>
        /// Retrieves a user by their ID
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/user/123
        /// </remarks>
        /// <param name="id">The unique identifier of the user</param>
        /// <returns>The user details</returns>
        /// <response code="200">Returns the requested user</response>
        /// <response code="400">If the ID is invalid</response>
        /// <response code="404">If the user was not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            try
            {
                logger.LogDebug("Retrieving user with ID: {UserId}", id);

                if (string.IsNullOrEmpty(id))
                {
                    logger.LogWarning("Invalid user ID provided: null or empty");
                    return BadRequest("User ID is required");
                }

                var user = await userService.GetByIdAsync(id);
                if (user == null)
                {
                    logger.LogWarning("User not found with ID: {UserId}", id);
                    return NotFound($"User with ID {id} not found");
                }

                logger.LogInformation("Successfully retrieved user with ID: {UserId}", id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        /// <summary>
        /// Retrieves all users
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/user
        /// </remarks>
        /// <returns>List of all users</returns>
        /// <response code="200">Returns the list of users</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ApiUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                logger.LogDebug("Retrieving all users");
                var users = await userService.GetAllAsync();
                logger.LogInformation("Successfully retrieved {Count} users", users.ToList().Count);
                return Ok(users);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        /// <summary>
        /// Creates a new user account
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/user
        ///     {
        ///         "email": "user@example.com",
        ///         "username": "johndoe",
        ///         "password": "SecurePassword123!"
        ///     }
        /// </remarks>
        /// <param name="user">The user details for registration</param>
        /// <returns>The created user</returns>
        /// <response code="201">User was successfully created</response>
        /// <response code="400">If the request data is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiUserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAsync([FromBody] ApiUserCreateDto user)
        {
            try
            {
                logger.LogDebug("Creating new user with email: {Email}", user?.Email);

                if (user == null)
                {
                    logger.LogWarning("Invalid request: user data is null");
                    return BadRequest("User data is required");
                }

                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state: {@ModelState}", ModelState);
                    return BadRequest(new { message = "Invalid user data", errors = ModelState });
                }

                var result = await userService.AddAsync(user);
                if (result)
                {
                    var createdUser = await userService.GetByEmailAsync(user.Email);
                    logger.LogInformation("Successfully created user with email: {Email}", user.Email);
                    return CreatedAtAction(nameof(GetByIdAsync), new { id = createdUser.Id }, createdUser);
                }

                logger.LogError("Failed to create user with email: {Email}", user.Email);
                return StatusCode(500, "Failed to create user");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user with email: {Email}", user?.Email);
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        /// <summary>
        /// Updates an existing user's information
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/v1/user/{id}
        ///     {
        ///         "id": "123",
        ///         "email": "updated@example.com",
        ///         "username": "janedoe"
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the user to update</param>
        /// <param name="user">The updated user information</param>
        /// <returns>Success indicator</returns>
        /// <response code="200">User was successfully updated</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="404">If the user was not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] ApiUserUpdateDto user)
        {
            try
            {
                if (id != user.Id)
                {
                    return BadRequest("ID mismatch between URL and body");
                }

                logger.LogDebug("Updating user with ID: {UserId}", user?.Id);

                if (user == null)
                {
                    logger.LogWarning("Invalid request: user data is null");
                    return BadRequest("User data is required");
                }

                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state: {@ModelState}", ModelState);
                    return BadRequest(new { message = "Invalid user data", errors = ModelState });
                }

                var result = await userService.UpdateAsync(user);
                if (result)
                {
                    logger.LogInformation("Successfully updated user with ID: {UserId}", user.Id);
                    return Ok(new { message = "User updated successfully" });
                }

                logger.LogWarning("User not found with ID: {UserId}", user.Id);
                return NotFound($"User with ID {user.Id} not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating user with ID: {UserId}", user?.Id);
                return StatusCode(500, "An error occurred while updating the user");
            }
        }

        /// <summary>
        /// Deletes a user account
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     DELETE /api/v1/user/{id}
        /// </remarks>
        /// <param name="id">The ID of the user to delete</param>
        /// <returns>Success indicator</returns>
        /// <response code="200">User was successfully deleted</response>
        /// <response code="404">If the user was not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            try
            {
                logger.LogDebug("Deleting user with ID: {UserId}", id);

                if (string.IsNullOrEmpty(id))
                {
                    logger.LogWarning("Invalid user ID provided: null or empty");
                    return BadRequest("User ID is required");
                }

                var result = await userService.DeleteByIdAsync(id);
                if (result)
                {
                    logger.LogInformation("Successfully deleted user with ID: {UserId}", id);
                    return Ok(new { message = "User deleted successfully" });
                }

                logger.LogWarning("User not found with ID: {UserId}", id);
                return NotFound($"User with ID {id} not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        /// <summary>
        /// Resets a user's password
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/user/reset-password
        ///     {
        ///         "token": "reset-token",
        ///         "newPassword": "NewSecurePassword123!"
        ///     }
        /// </remarks>
        /// <param name="resetPasswordInfo">The reset token and new password</param>
        /// <returns>Result of the password reset operation</returns>
        /// <response code="200">Password was successfully reset</response>
        /// <response code="400">If the request data is invalid</response>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ResetPasswordAsync([FromBody] ResetPasswordDto resetPasswordInfo)
        {
            try
            {
                logger.LogDebug("Processing password reset request");

                if (resetPasswordInfo == null)
                {
                    logger.LogWarning("Invalid request: reset password data is null");
                    return BadRequest("Reset password data is required");
                }

                if (string.IsNullOrEmpty(resetPasswordInfo.Token))
                {
                    logger.LogWarning("Reset password token is missing");
                    return BadRequest("Reset token is required");
                }

                var response = await userService.ResetPasswordAsync(resetPasswordInfo);
                if ((bool)response)
                {
                    logger.LogInformation("Password reset successful");
                    return Ok(new { message = "Password reset successful" });
                }

                logger.LogWarning("Password reset failed");
                return BadRequest("Password reset failed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing password reset");
                return StatusCode(500, "An error occurred while processing the password reset");
            }
        }

        /// <summary>
        /// Confirms a user's email address
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/user/email-confirmation/{token}/{email}
        /// </remarks>
        /// <param name="token">The email confirmation token</param>
        /// <param name="email">The email address to confirm</param>
        /// <returns>Result of the email confirmation operation</returns>
        /// <response code="200">Email was successfully confirmed</response>
        /// <response code="400">If the request data is invalid</response>
        [HttpGet("email-confirmation/{token}/{email}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> EmailConfirmationAsync(string token, string email)
        {
            try
            {
                logger.LogDebug("Processing email confirmation for: {Email}", email);

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                {
                    logger.LogWarning("Invalid request: token or email is missing");
                    return BadRequest("Token and email are required");
                }

                var response = await userService.EmailConfirmationAsync(new EmailConfirmationDto { Email = email, Token = token });
                if (response)
                {
                    logger.LogInformation("Email confirmation successful for: {Email}", email);
                    return Ok(new { message = "Email confirmed successfully" });
                }

                logger.LogWarning("Email confirmation failed for: {Email}", email);
                return BadRequest("Email confirmation failed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error confirming email for: {Email}", email);
                return StatusCode(500, "An error occurred while confirming the email");
            }
        }

        /// <summary>
        /// Resends the email confirmation link
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/user/resend-confirmation?email=user@example.com
        /// </remarks>
        /// <param name="email">The email address to resend confirmation to</param>
        /// <returns>Result of the resend operation</returns>
        /// <response code="200">Confirmation email was successfully sent</response>
        /// <response code="400">If the email is invalid or not found</response>
        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResendConfirmationEmailAsync([FromQuery] string email)
        {
            try
            {
                logger.LogDebug("Resending confirmation email to: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    logger.LogWarning("Invalid request: email is missing");
                    return BadRequest("Email address is required");
                }

                var result = await userService.ResendConfirmationEmailAsync(email);
                if (result)
                {
                    logger.LogInformation("Confirmation email resent successfully to: {Email}", email);
                    return Ok(new { message = "Confirmation email sent successfully" });
                }

                logger.LogWarning("Failed to resend confirmation email to: {Email}", email);
                return BadRequest("Failed to send confirmation email");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resending confirmation email to: {Email}", email);
                return StatusCode(500, "An error occurred while sending the confirmation email");
            }
        }

        /// <summary>
        /// Retrieves a user by their email address
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/user/email/user@example.com
        /// </remarks>
        /// <param name="email">The email address of the user</param>
        /// <returns>The user details</returns>
        /// <response code="200">Returns the requested user</response>
        /// <response code="400">If the email is invalid</response>
        /// <response code="404">If the user was not found</response>
        [HttpGet("email/{email}")]
        [ProducesResponseType(typeof(ApiUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByEmailAsync(string email)
        {
            try
            {
                logger.LogDebug("Retrieving user with email: {Email}", email);

                if (string.IsNullOrEmpty(email))
                {
                    logger.LogWarning("Invalid email provided: null or empty");
                    return BadRequest("Email is required");
                }

                var user = await userService.GetByEmailAsync(email);
                if (user == null)
                {
                    logger.LogWarning("User not found with email: {Email}", email);
                    return NotFound($"User with email {email} not found");
                }

                logger.LogInformation("Successfully retrieved user with email: {Email}", email);
                return Ok(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving user with email: {Email}", email);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }
    }
}

