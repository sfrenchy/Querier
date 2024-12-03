using Querier.Api.Models.Auth;
using Querier.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for handling authentication operations
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - User registration (SignUp)
    /// - User authentication (SignIn)
    /// - Token management (RefreshToken)
    /// 
    /// ## Authentication
    /// Most endpoints in this controller do not require authentication as they are used to obtain authentication tokens.
    /// For endpoints that return tokens, use the token in subsequent requests:
    /// ```
    /// Authorization: Bearer {your-jwt-token}
    /// ```
    /// 
    /// ## Common Responses
    /// - 200 OK: Operation completed successfully
    /// - 400 Bad Request: Invalid payload or request
    /// - 401 Unauthorized: Invalid credentials
    /// - 500 Internal Server Error: Unexpected server error
    /// </remarks>
    [Route("api/v1/[controller]")] // api/authmanagement
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class AuthManagementController : ControllerBase
    {
        private readonly ILogger _logger;
        private IAuthManagementService _authManagementService;

        public AuthManagementController(IAuthManagementService authManagementService, ILogger<AuthManagementController> logger)
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
        [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest user)
        {
            if (ModelState.IsValid)
            {
                return Ok(await _authManagementService.SignUp(user));
            }
            return BadRequest(new SignUpResponse()
            {
                Success = false,
                Errors = new List<string>()
                {
                    "Invalid payload"
                }
            });
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
        [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest user)
        {
            if (ModelState.IsValid)
            {
                return Ok(await _authManagementService.SignIn(user));
            }
            return BadRequest(new SignUpResponse()
            {
                Success = false,
                Errors = new List<string>()
                {
                    "Invalid payload"
                }
            });
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
        [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (ModelState.IsValid)
            {
                return Ok(await _authManagementService.RefreshToken(tokenRequest));
            }

            return BadRequest(new SignUpResponse()
            {
                Errors = new List<string>() {
                    "Invalid payload"
                },
                Success = false
            });
        }
    }
}
