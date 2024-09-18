using Querier.Api.Models.Auth;
using Querier.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Route("api/[controller]")] // api/authmanagement
    [ApiController]
    public class AuthManagementController : ControllerBase
    {
        private readonly ILogger _logger;
        private IAuthManagementService _authManagementService;

        public AuthManagementController(IAuthManagementService authManagementService, ILogger<AuthManagementController> logger)
        {
            _authManagementService = authManagementService;
            _logger = logger;
        }

        [HttpPost]
        [Route("SignUp")]
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

        // [HttpPost]
        // [Route("GoogleAuth")]
        // public async Task<IActionResult> LoginFromGoogle([FromBody] GoogleLoginRequest user)
        // {
        //     if (ModelState.IsValid)
        //     {
        //         return Ok(await _authManagementService.GoogleLogin(user));
        //     }
        //     return BadRequest(new SignUpResponse()
        //     {
        //         Success = false,
        //         Errors = new List<string>()
        //         {
        //             "Invalid payload"
        //         }
        //     });
        // }

        [HttpPost]
        [Route("SignIn")]
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

        [HttpPost]
        [Route("RefreshToken")]
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
