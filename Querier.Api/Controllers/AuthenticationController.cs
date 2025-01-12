using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Services;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller responsible for managing user authentication and authorization
    /// </summary>
    /// <remarks>
    /// This controller handles all authentication-related operations including:
    /// - User registration (SignUp)
    /// - User authentication (SignIn)
    /// - Token management (refresh)
    /// - Session handling
    /// 
    /// All endpoints return standardized responses with appropriate HTTP status codes
    /// and follow RESTful conventions.
    /// 
    /// ## Password Requirements
    /// - Minimum length: 12 characters
    /// - Must contain at least one uppercase letter
    /// - Must contain at least one lowercase letter
    /// - Must contain at least one number
    /// - Must contain at least one special character
    /// - Must not contain common patterns or dictionary words
    /// </remarks>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthenticationController(
        IAuthenticationService authManagementService,
        ILogger<AuthenticationController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Registers a new user in the system
        /// </summary>
        /// <remarks>
        /// Creates a new user account and generates authentication tokens upon successful registration.
        /// 
        /// Sample request:
        ///     POST /api/v1/authentication/signup
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "StrongP@ssw0rd123!"
        ///     }
        /// 
        /// Sample success response:
        ///     {
        ///         "success": true,
        ///         "token": "eyJhbGciOiJIUzI1NiIs...",
        ///         "refreshToken": "6e7c8f9a-b1c2-3d4e-5f6g...",
        ///         "errors": null
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "success": false,
        ///         "token": null,
        ///         "refreshToken": null,
        ///         "errors": [
        ///             "Email 'user@example.com' is already taken",
        ///             "Password does not meet complexity requirements"
        ///         ]
        ///     }
        /// </remarks>
        /// <param name="user">User registration details containing email and password</param>
        /// <returns>Registration result containing authentication tokens if successful</returns>
        /// <response code="200">Registration successful, returns tokens</response>
        /// <response code="400">Invalid registration details or validation errors</response>
        /// <response code="500">Internal server error during registration</response>
        [HttpPost]
        [Route("SignUp")]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SignUp([FromBody] SignUpDto user)
        {
            logger.LogInformation("Attempting to sign up user with email: {Email}", user.Email);

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid signup model state for email: {Email}", user.Email);
                return BadRequest(new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid registration details" }
                });
            }

            try
            {
                var result = await authManagementService.SignUp(user);
                if (result.Success)
                {
                    logger.LogInformation("User successfully signed up: {Email}", user.Email);
                    return Ok(result);
                }

                logger.LogWarning("Failed to sign up user: {Email}. Errors: {@Errors}", user.Email, result.Errors);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during signup for email: {Email}", user.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "An unexpected error occurred during registration" }
                });
            }
        }

        /// <summary>
        /// Authenticates an existing user
        /// </summary>
        /// <remarks>
        /// Validates user credentials and issues authentication tokens upon successful authentication.
        /// 
        /// Sample request:
        ///     POST /api/v1/authentication/signin
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "YourPassword123!"
        ///     }
        /// 
        /// Sample success response:
        ///     {
        ///         "success": true,
        ///         "token": "eyJhbGciOiJIUzI1NiIs...",
        ///         "refreshToken": "6e7c8f9a-b1c2-3d4e-5f6g...",
        ///         "errors": null
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "success": false,
        ///         "token": null,
        ///         "refreshToken": null,
        ///         "errors": [
        ///             "Invalid email or password"
        ///         ]
        ///     }
        /// </remarks>
        /// <param name="user">User credentials containing email and password</param>
        /// <returns>Authentication result containing tokens if successful</returns>
        /// <response code="200">Authentication successful, returns tokens</response>
        /// <response code="400">Invalid credentials or validation errors</response>
        /// <response code="500">Internal server error during authentication</response>
        [HttpPost]
        [Route("SignIn")]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SignIn([FromBody] SignInDto user)
        {
            logger.LogInformation("Attempting to sign in user: {Email}", user.Email);

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid signin model state for email: {Email}", user.Email);
                return BadRequest(new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid login details" }
                });
            }

            try
            {
                var result = await authManagementService.SignIn(user);
                if (result.Success)
                {
                    logger.LogInformation("User successfully signed in: {Email}", user.Email);
                    return Ok(result);
                }

                logger.LogWarning("Failed login attempt for user: {Email}", user.Email);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during signin for email: {Email}", user.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "An unexpected error occurred during login" }
                });
            }
        }

        /// <summary>
        /// Refreshes an expired JWT token using a valid refresh token
        /// </summary>
        /// <remarks>
        /// Issues a new set of authentication tokens when provided with valid existing tokens.
        /// This endpoint should be called when the JWT token expires but the refresh token is still valid.
        /// 
        /// Sample request:
        ///     POST /api/v1/authentication/refreshtoken
        ///     {
        ///         "token": "eyJhbGciOiJIUzI1NiIs...",
        ///         "refreshToken": "6e7c8f9a-b1c2-3d4e-5f6g..."
        ///     }
        /// 
        /// Sample success response:
        ///     {
        ///         "success": true,
        ///         "token": "eyJhbGciOiJIUzI1NiIs...",
        ///         "refreshToken": "7f8d9e0a-c2d3-4e5f-6g7h...",
        ///         "errors": null
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "success": false,
        ///         "token": null,
        ///         "refreshToken": null,
        ///         "errors": [
        ///             "Invalid token",
        ///             "Refresh token has expired"
        ///         ]
        ///     }
        /// </remarks>
        /// <param name="tokenRequest">Current JWT and refresh tokens</param>
        /// <returns>New authentication tokens if refresh is successful</returns>
        /// <response code="200">Token refresh successful, returns new tokens</response>
        /// <response code="400">Invalid or expired tokens</response>
        /// <response code="500">Internal server error during token refresh</response>
        [HttpPost]
        [Route("RefreshToken")]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto tokenRequest)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid refresh token request");
                return BadRequest(new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid token request" }
                });
            }

            try
            {
                var result = await authManagementService.RefreshToken(tokenRequest);
                if (result == null)
                {
                    logger.LogWarning("Token refresh failed - invalid or expired token");
                    return BadRequest(new AuthResultDto
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid token" }
                    });
                }

                if (result.Success)
                {
                    logger.LogInformation("Token successfully refreshed");
                    return Ok(result);
                }

                logger.LogWarning("Token refresh failed. Errors: {@Errors}", result.Errors);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during token refresh");
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "An unexpected error occurred during token refresh" }
                });
            }
        }

        /// <summary>
        /// Signs out the current user by invalidating their refresh token
        /// </summary>
        /// <remarks>
        /// Invalidates the user's current refresh token, effectively ending their session.
        /// This should be called when the user wants to log out of the application.
        /// 
        /// Sample request:
        ///     POST /api/v1/authentication/signout
        ///     {
        ///         "refreshToken": "6e7c8f9a-b1c2-3d4e-5f6g..."
        ///     }
        /// 
        /// Sample success response:
        ///     {
        ///         "success": true,
        ///         "errors": null
        ///     }
        /// 
        /// Sample error response:
        ///     {
        ///         "success": false,
        ///         "errors": [
        ///             "Invalid refresh token"
        ///         ]
        ///     }
        /// </remarks>
        /// <param name="tokenRequest">The refresh token to invalidate</param>
        /// <returns>Result indicating whether the sign out was successful</returns>
        /// <response code="200">Sign out successful</response>
        /// <response code="400">Invalid refresh token</response>
        /// <response code="500">Internal server error during sign out</response>
        [HttpPost]
        [Route("SignOut")]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SignOut([FromBody] RefreshTokenDto tokenRequest)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid sign out request");
                return BadRequest(new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid token" }
                });
            }

            try
            {
                var result = await authManagementService.SignOut(tokenRequest);
                if (result.Success)
                {
                    logger.LogInformation("User successfully signed out");
                    return Ok(result);
                }

                logger.LogWarning("Sign out failed. Errors: {@Errors}", result.Errors);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during sign out");
                return StatusCode(StatusCodes.Status500InternalServerError, new AuthResultDto
                {
                    Success = false,
                    Errors = new List<string> { "An unexpected error occurred during sign out" }
                });
            }
        }
    }
}
