using System;
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
        [HttpPut("Add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddAsync([FromBody] ApiUserCreateDto user)
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
                    logger.LogInformation("Successfully created user with email: {Email}", user.Email);
                    return Ok(new { message = "User created successfully" });
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
        ///     PUT /api/v1/usermanagement/update
        ///     {
        ///         "id": "123",
        ///         "email": "updated@example.com",
        ///         "username": "janedoe"
        ///     }
        /// </remarks>
        /// <param name="user">The updated user information</param>
        /// <returns>Success indicator</returns>
        [HttpPut("Update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAsync([FromBody] ApiUserUpdateDto user)
        {
            try
            {
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

                logger.LogError("Failed to update user with ID: {UserId}", user.Id);
                return StatusCode(500, "Failed to update user");
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
        ///     DELETE /api/v1/usermanagement/delete/123
        /// </remarks>
        /// <param name="id">The ID of the user to delete</param>
        /// <returns>Success indicator</returns>
        [HttpDelete("Delete/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state: {@ModelState}", ModelState);
                    return BadRequest(new { message = "Invalid request", errors = ModelState });
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
        /// Retrieves all users without pagination
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/usermanagement/getall
        /// </remarks>
        /// <returns>Complete list of users</returns>
        [HttpGet("GetAll")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
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
        /// <param name="resetPasswordInfo">The reset token and new password</param>
        /// <returns>Result of the password reset operation</returns>
        [HttpPost("resetPassword")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordInfo)
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
        ///     GET /api/v1/usermanagement/emailconfirmation/confirmation-token/user@example.com
        /// </remarks>
        /// <param name="token">The email confirmation token</param>
        /// <param name="mail">The email address to confirm</param>
        /// <returns>Result of the email confirmation operation</returns>
        [HttpGet("emailConfirmation/{token}/{mail}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> EmailConfirmation(string token, string mail)
        {
            try
            {
                logger.LogDebug("Processing email confirmation for: {Email}", mail);

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(mail))
                {
                    logger.LogWarning("Invalid request: token or email is missing");
                    return BadRequest("Token and email are required");
                }

                var response = await userService.EmailConfirmationAsync(new EmailConfirmationDto { Email = mail, Token = token });
                if (response)
                {
                    logger.LogInformation("Email confirmation successful for: {Email}", mail);
                    return Ok(new { message = "Email confirmed successfully" });
                }

                logger.LogWarning("Email confirmation failed for: {Email}", mail);
                return BadRequest("Email confirmation failed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error confirming email for: {Email}", mail);
                return StatusCode(500, "An error occurred while confirming the email");
            }
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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Me()
        {
            try
            {
                logger.LogDebug("Retrieving current user profile");
                var user = await userService.GetCurrentUserAsync(User);
                
                if (user == null)
                {
                    logger.LogWarning("Current user not found");
                    return NotFound("Current user not found");
                }

                logger.LogInformation("Successfully retrieved current user profile: {UserId}", user.Id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving current user profile");
                return StatusCode(500, "An error occurred while retrieving the current user profile");
            }
        }

        [HttpPost("resend-confirmation")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResendConfirmationEmail([FromQuery] string userEmail)
        {
            try
            {
                logger.LogDebug("Resending confirmation email to: {Email}", userEmail);

                if (string.IsNullOrEmpty(userEmail))
                {
                    logger.LogWarning("Invalid request: email is missing");
                    return BadRequest("Email address is required");
                }

                var result = await userService.ResendConfirmationEmailAsync(userEmail);
                if (result)
                {
                    logger.LogInformation("Confirmation email resent successfully to: {Email}", userEmail);
                    return Ok(new { message = "Confirmation email sent successfully" });
                }

                logger.LogWarning("Failed to resend confirmation email to: {Email}", userEmail);
                return BadRequest("Failed to send confirmation email");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resending confirmation email to: {Email}", userEmail);
                return StatusCode(500, "An error occurred while sending the confirmation email");
            }
        }
    }
}

