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
    /// Controller for managing user accounts and profiles
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - User account management (CRUD operations)
    /// - Profile updates and settings
    /// - User preferences
    /// - Account status management
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
    /// - 201 Created: Resource created successfully
    /// - 204 No Content: Operation completed successfully with no response body
    /// - 400 Bad Request: Invalid input data
    /// - 401 Unauthorized: Authentication required
    /// - 403 Forbidden: User lacks required permissions
    /// - 404 Not Found: Resource not found
    /// - 500 Internal Server Error: Unexpected server error
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
        /// 
        /// Sample success response:
        ///     {
        ///         "id": "123",
        ///         "email": "user@example.com",
        ///         "username": "johndoe",
        ///         "roles": ["User", "Admin"],
        ///         "isEmailConfirmed": true,
        ///         "createdAt": "2024-03-19T10:30:00Z",
        ///         "lastLoginAt": "2024-03-19T15:45:00Z"
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        ///         "title": "Not Found",
        ///         "status": 404,
        ///         "detail": "User with ID 123 not found"
        ///     }
        /// </remarks>
        /// <param name="id">The unique identifier of the user</param>
        /// <returns>The user details</returns>
        /// <response code="200">Returns the requested user</response>
        /// <response code="400">If the ID is invalid</response>
        /// <response code="404">If the user was not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            try
            {
                logger.LogDebug("Retrieving user with ID: {UserId}", id);

                if (string.IsNullOrEmpty(id))
                {
                    logger.LogWarning("Invalid user ID provided: null or empty");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Invalid User ID",
                        Detail = "User ID is required and cannot be empty",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var user = await userService.GetByIdAsync(id);
                if (user == null)
                {
                    logger.LogWarning("User not found with ID: {UserId}", id);
                    return NotFound(new ProblemDetails
                    {
                        Title = "User Not Found",
                        Detail = $"User with ID {id} not found",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                logger.LogInformation("Successfully retrieved user with ID: {UserId}", id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving the user",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Retrieves all users
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/user
        /// 
        /// Sample success response:
        ///     [
        ///         {
        ///             "id": "123",
        ///             "email": "user1@example.com",
        ///             "username": "johndoe",
        ///             "roles": ["User"],
        ///             "isEmailConfirmed": true
        ///         },
        ///         {
        ///             "id": "456",
        ///             "email": "user2@example.com",
        ///             "username": "janedoe",
        ///             "roles": ["Admin"],
        ///             "isEmailConfirmed": true
        ///         }
        ///     ]
        /// </remarks>
        /// <returns>List of all users</returns>
        /// <response code="200">Returns the list of users</response>
        /// <response code="500">If there was an unexpected error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ApiUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving users",
                    Status = StatusCodes.Status500InternalServerError
                });
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
        /// 
        /// Sample success response:
        ///     {
        ///         "id": "123",
        ///         "email": "user@example.com",
        ///         "username": "johndoe",
        ///         "roles": ["User"],
        ///         "isEmailConfirmed": false
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        ///         "title": "Bad Request",
        ///         "status": 400,
        ///         "detail": "Invalid user data",
        ///         "errors": {
        ///             "email": ["The Email field is required"],
        ///             "password": ["Password must be at least 8 characters"]
        ///         }
        ///     }
        /// </remarks>
        /// <param name="user">The user details for registration</param>
        /// <returns>The created user</returns>
        /// <response code="201">User was successfully created</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="500">If there was an unexpected error</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiUserDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAsync([FromBody] ApiUserCreateDto user)
        {
            try
            {
                logger.LogDebug("Creating new user with email: {Email}", user?.Email);

                if (user == null)
                {
                    logger.LogWarning("Invalid request: user data is null");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = "User data is required",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state: {@ModelState}", ModelState);
                    return BadRequest(new ValidationProblemDetails(ModelState)
                    {
                        Title = "Invalid User Data",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var result = await userService.AddAsync(user);
                if (result)
                {
                    var createdUser = await userService.GetByEmailAsync(user.Email);
                    logger.LogInformation("Successfully created user with email: {Email}", user.Email);
                    return Created($"/api/v1/user/{createdUser.Id}", createdUser);
                }

                logger.LogError("Failed to create user with email: {Email}", user.Email);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Creation Failed",
                    Detail = "Failed to create user",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating user with email: {Email}", user?.Email);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while creating the user",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Updates an existing user's information
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/v1/user/123
        ///     {
        ///         "id": "123",
        ///         "email": "updated@example.com",
        ///         "username": "janedoe"
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        ///         "title": "Bad Request",
        ///         "status": 400,
        ///         "detail": "ID mismatch between URL and body"
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the user to update</param>
        /// <param name="user">The updated user information</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">User was successfully updated</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="404">If the user was not found</response>
        /// <response code="500">If there was an unexpected error</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] ApiUserUpdateDto user)
        {
            try
            {
                if (id != user.Id)
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "ID Mismatch",
                        Detail = "ID in URL must match ID in request body",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                logger.LogDebug("Updating user with ID: {UserId}", user?.Id);

                if (user == null)
                {
                    logger.LogWarning("Invalid request: user data is null");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = "User data is required",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state: {@ModelState}", ModelState);
                    return BadRequest(new ValidationProblemDetails(ModelState)
                    {
                        Title = "Invalid User Data",
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                var result = await userService.UpdateAsync(user);
                if (result)
                {
                    logger.LogInformation("Successfully updated user with ID: {UserId}", user.Id);
                    return NoContent();
                }

                logger.LogWarning("User not found with ID: {UserId}", user.Id);
                return NotFound(new ProblemDetails
                {
                    Title = "User Not Found",
                    Detail = $"User with ID {user.Id} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating user with ID: {UserId}", user?.Id);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while updating the user",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Deletes a user account
        /// </summary>
        /// <remarks>
        /// This operation permanently removes a user account and cannot be undone.
        /// 
        /// Sample request:
        ///     DELETE /api/v1/user/123
        /// 
        /// Sample error response:
        ///     {
        ///         "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        ///         "title": "Not Found",
        ///         "status": 404,
        ///         "detail": "User with ID 123 not found"
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the user to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">User was successfully deleted</response>
        /// <response code="404">If the user was not found</response>
        /// <response code="500">If there was an unexpected error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            try
            {
                logger.LogDebug("Deleting user with ID: {UserId}", id);

                var result = await userService.DeleteByIdAsync(id);
                if (result)
                {
                    logger.LogInformation("Successfully deleted user with ID: {UserId}", id);
                    return NoContent();
                }

                logger.LogWarning("User not found with ID: {UserId}", id);
                return NotFound(new ProblemDetails
                {
                    Title = "User Not Found",
                    Detail = $"User with ID {id} not found",
                    Status = StatusCodes.Status404NotFound
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while deleting the user",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Retrieves the currently authenticated user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/user/me
        /// 
        /// Sample success response:
        ///     {
        ///         "id": "123",
        ///         "email": "user@example.com",
        ///         "username": "johndoe",
        ///         "roles": ["User", "Admin"],
        ///         "isEmailConfirmed": true,
        ///         "createdAt": "2024-03-19T10:30:00Z",
        ///         "lastLoginAt": "2024-03-19T15:45:00Z"
        ///     }
        /// </remarks>
        /// <returns>The current user's details</returns>
        /// <response code="200">Returns the current user</response>
        /// <response code="404">If the user was not found</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUserAsync()
        {
            try
            {
                logger.LogDebug("Retrieving current user");
                var user = await userService.GetCurrentUserAsync(User);

                if (user == null)
                {
                    logger.LogWarning("Current user not found");
                    return NotFound(new ProblemDetails
                    {
                        Title = "User Not Found",
                        Detail = "Current user not found",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                logger.LogInformation("Successfully retrieved current user: {Email}", user.Email);
                return Ok(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving current user");
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving the current user",
                    Status = StatusCodes.Status500InternalServerError
                });
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

