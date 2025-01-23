using System;
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
    public class EmailConfirmationController(
        IUserService userService,
        ILogger<EmailConfirmationController> logger)
        : Controller
    {
        /// <summary>
        /// Displays the email confirmation page
        /// </summary>
        [HttpGet("/email_confirmation")]
        public IActionResult Index([FromQuery] string token, [FromQuery] string mail)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(mail))
                {
                    logger.LogWarning("Invalid confirmation attempt: missing token or email");
                    return BadRequest("Token et email requis");
                }

                logger.LogInformation("Displaying email confirmation page for {Email}", mail);
                ViewData["Token"] = token;
                ViewData["Email"] = mail;
                return View();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error displaying email confirmation page for {Email}", mail);
                return StatusCode(500, "Une erreur est survenue");
            }
        }

        /// <summary>
        /// Processes the email confirmation
        /// </summary>
        [HttpPost("/email_confirmation")]
        public async Task<IActionResult> Confirm([FromForm] EmailConfirmationSetPasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    logger.LogWarning("Invalid model state for email confirmation: {@ModelState}", ModelState);
                    return View("Index", request);
                }

                logger.LogInformation("Processing email confirmation for {Email}", request.Email);
                request.ConfirmPassword = request.Password;
                
                var result = await userService.ConfirmEmailAndSetPasswordAsync(request);
                if (result.Succeeded)
                {
                    logger.LogInformation("Email confirmation successful for {Email}", request.Email);
                    ViewData["Success"] = true;
                    return View("Success");
                }

                logger.LogWarning("Email confirmation failed for {Email}: {Error}", request.Email, result.Error);
                ViewData["Error"] = result.Error;
                ViewData["Token"] = request.Token;
                ViewData["Email"] = request.Email;
                return View("Index");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing email confirmation for {Email}", request?.Email);
                ViewData["Error"] = "Une erreur inattendue est survenue";
                ViewData["Token"] = request?.Token;
                ViewData["Email"] = request?.Email;
                return View("Index");
            }
        }
    }
} 