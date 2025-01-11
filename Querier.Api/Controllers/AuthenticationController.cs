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
    /// Controller for handling user authentication
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - User login and authentication
    /// - Token management
    /// - Password reset functionality
    /// - Session management
    /// </remarks>
    [Route("api/v1/[controller]")] // api/authmanagement
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationController> _logger;
        private readonly IAuthenticationService _authManagementService;

        public AuthenticationController(IAuthenticationService authManagementService, ILogger<AuthenticationController> logger)
        {
            _authManagementService = authManagementService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <remarks>
        /// Creates a new user account with the provided credentials.
        /// 
        /// Sample request:
        ///     POST /api/v1/authmanagement/signup
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "StrongPassword123!"
        ///     }
        /// </remarks>
        /// <param name="user">The user registration details</param>
        /// <returns>Registration result with authentication tokens if successful</returns>
        /// <response code="200">Returns the registration result with tokens</response>
        /// <response code="400">If the registration details are invalid</response>
        [HttpPost]
        [Route("SignUp")]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignUp([FromBody] SignUpDto user)
        {
            _logger.LogInformation("Attempting to sign up user with email: {Email}", user.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid signup model state for email: {Email}", user.Email);
                return BadRequest(new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid registration details" }
                });
            }

            try
            {
                var result = await _authManagementService.SignUp(user);
                if (result.Success)
                {
                    _logger.LogInformation("User successfully signed up: {Email}", user.Email);
                    return Ok(result);
                }

                _logger.LogWarning("Failed to sign up user: {Email}. Errors: {@Errors}", user.Email, result.Errors);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during signup for email: {Email}", user.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "An unexpected error occurred during registration" }
                });
            }
        }

        /// <summary>
        /// Authenticate a user
        /// </summary>
        /// <remarks>
        /// Authenticates a user with their credentials and returns authentication tokens.
        /// 
        /// Sample request:
        ///     POST /api/v1/authmanagement/signin
        ///     {
        ///         "email": "user@example.com",
        ///         "password": "YourPassword123!"
        ///     }
        /// </remarks>
        /// <param name="user">The user credentials</param>
        /// <returns>Authentication result with tokens if successful</returns>
        /// <response code="200">Returns the authentication tokens</response>
        /// <response code="400">If the credentials are invalid</response>
        [HttpPost]
        [Route("SignIn")]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignIn([FromBody] SignInDto user)
        {
            _logger.LogInformation("Attempting to sign in user: {Email}", user.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid signin model state for email: {Email}", user.Email);
                return BadRequest(new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid login details" }
                });
            }

            try
            {
                var result = await _authManagementService.SignIn(user);
                if (result.Success)
                {
                    _logger.LogInformation("User successfully signed in: {Email}", user.Email);
                    return Ok(result);
                }

                _logger.LogWarning("Failed login attempt for user: {Email}", user.Email);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during signin for email: {Email}", user.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "An unexpected error occurred during login" }
                });
            }
        }

        /// <summary>
        /// Refresh an authentication token
        /// </summary>
        /// <remarks>
        /// Generates a new JWT token using a valid refresh token.
        /// 
        /// Sample request:
        ///     POST /api/v1/authmanagement/refreshtoken
        ///     {
        ///         "token": "current-jwt-token",
        ///         "refreshToken": "current-refresh-token"
        ///     }
        /// </remarks>
        /// <param name="tokenRequest">The current tokens</param>
        /// <returns>New authentication tokens if successful</returns>
        /// <response code="200">Returns new authentication tokens</response>
        /// <response code="400">If the tokens are invalid</response>
        [HttpPost]
        [Route("RefreshToken")]
        [ProducesResponseType(typeof(SignUpResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid refresh token request");
                return BadRequest(new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "Invalid token request" }
                });
            }

            try
            {
                var result = await _authManagementService.RefreshToken(tokenRequest);
                if (result == null)
                {
                    _logger.LogWarning("Token refresh failed - invalid or expired token");
                    return BadRequest(new SignUpResultDto
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid token" }
                    });
                }

                if (result.Success)
                {
                    _logger.LogInformation("Token successfully refreshed");
                    return Ok(result);
                }

                _logger.LogWarning("Token refresh failed. Errors: {@Errors}", result.Errors);
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                return StatusCode(StatusCodes.Status500InternalServerError, new SignUpResultDto
                {
                    Success = false,
                    Errors = new List<string> { "An unexpected error occurred during token refresh" }
                });
            }
        }
    }
}
