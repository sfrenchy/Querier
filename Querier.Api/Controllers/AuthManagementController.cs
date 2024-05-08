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

        //[HttpPost]
        //[Route("Register")]
        //public async Task<IActionResult> Register([FromBody] UserRegistrationRequest user)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        return Ok(await _authManagementService.Register(user));
        //    }
        //    return BadRequest(new RegistrationResponse()
        //    {
        //        Success = false,
        //        Errors = new List<string>()
        //        {
        //            "Invalid payload"
        //        }
        //    });
        //}

        [HttpPost]
        [Route("GoogleAuth")]
        public async Task<IActionResult> LoginFromGoogle([FromBody] GoogleLoginRequest user)
        {
            if (ModelState.IsValid)
            {
                return Ok(await _authManagementService.GoogleLogin(user));
            }
            return BadRequest(new RegistrationResponse()
            {
                Success = false,
                Errors = new List<string>()
                {
                    "Invalid payload"
                }
            });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest user)
        {
            if (ModelState.IsValid)
            {
                return Ok(await _authManagementService.Login(user));
            }
            return BadRequest(new RegistrationResponse()
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

            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>() {
                "Invalid payload"
            },
                Success = false
            });
        }
    }
}
