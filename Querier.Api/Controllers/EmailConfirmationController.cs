using Microsoft.AspNetCore.Mvc;
using Querier.Api.Services.User;
using System;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    public class EmailConfirmationController : Controller
    {
        private readonly IUserService _userService;

        public EmailConfirmationController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("/email_confirmation")]
        public IActionResult Index([FromQuery] string token, [FromQuery] string mail)
        {
            ViewData["Token"] = Uri.UnescapeDataString(token);
            ViewData["Email"] = mail;
            return View();
        }

        [HttpPost("/email_confirmation")]
        public async Task<IActionResult> Confirm([FromForm] EmailConfirmationRequest request)
        {
            request.ConfirmPassword = request.Password;
            
            var result = await _userService.ConfirmEmailAndSetPassword(request);
            if (result.Succeeded)
            {
                ViewData["Success"] = true;
                return View("Success");
            }

            ViewData["Error"] = result.Error;
            ViewData["Token"] = request.Token;
            ViewData["Email"] = request.Email;
            return View("Index");
        }
    }
} 