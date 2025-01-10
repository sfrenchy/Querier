using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Net;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Services;

namespace Querier.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class SmtpController : ControllerBase
    {
        private readonly ILogger<SmtpController> _logger;
        private readonly IEmailSendingService _emailService;

        public SmtpController(
            ILogger<SmtpController> logger,
            IEmailSendingService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        /// <summary>
        /// Test SMTP configuration
        /// </summary>
        /// <remarks>
        /// This endpoint tests if the SMTP configuration is valid by attempting to connect to the SMTP server.
        /// This endpoint is accessible without authentication only during initial setup.
        /// </remarks>
        /// <response code="200">If the connection test is successful</response>
        /// <response code="400">If the connection test fails</response>
        /// <response code="403">If the application is already configured</response>
        [HttpPost("test")]
        [AllowAnonymous]
        public async Task<IActionResult> TestConfiguration([FromBody] SmtpTestDto request)
        {
            try
            {
                if (await _emailService.IsConfigured())
                {
                    return StatusCode((int)HttpStatusCode.Forbidden, new { error = "Access denied. Application is already configured." });
                }

                await _emailService.TestSmtpConfiguration(request);
                return Ok(new { message = "SMTP configuration test successful" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"SMTP test failed: {ex.Message}" });
            }
        }
    }
} 