using Microsoft.AspNetCore.Mvc;
using Querier.Api.Services.User;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Controllers
{
    public class EmailConfirmationController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<EmailConfirmationController> _logger;

        public EmailConfirmationController(IUserService userService, ILogger<EmailConfirmationController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("/email_confirmation")]
        public IActionResult Index([FromQuery] string token, [FromQuery] string mail)
        {
            ViewData["Token"] = token;
            ViewData["Email"] = mail;
            return View();
        }

        [HttpPost("/email_confirmation")]
        public async Task<IActionResult> Confirm([FromForm] EmailConfirmationRequest request)
        {
            _logger.LogInformation($"Attempting to confirm email for {request.Email}");
            request.ConfirmPassword = request.Password;
            
            var result = await _userService.ConfirmEmailAndSetPassword(request);
            if (result.Succeeded)
            {
                ViewData["Success"] = true;
                return View("Success");
            }

            _logger.LogWarning($"Email confirmation failed for {request.Email}: {result.Error}");
            ViewData["Error"] = result.Error;
            ViewData["Token"] = request.Token;
            ViewData["Email"] = request.Email;
            return View("Index");
        }
    }
} 