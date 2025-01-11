using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing email confirmation processes
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Handling email verification
    /// - Managing confirmation tokens
    /// - Processing email confirmations
    /// - Resending confirmation emails
    /// </remarks>
    [Route("api/v1/[controller]")]
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
        public async Task<IActionResult> Confirm([FromForm] EmailConfirmationSetPasswordDto request)
        {
            _logger.LogInformation($"Attempting to confirm email for {request.Email}");
            request.ConfirmPassword = request.Password;
            
            var result = await _userService.ConfirmEmailAndSetPasswordAsync(request);
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