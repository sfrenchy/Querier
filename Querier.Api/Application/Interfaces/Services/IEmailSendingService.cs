using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Application.Interfaces.Services;

public interface IEmailSendingService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);
    Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, string language, Dictionary<string, string> parameters);
    Task<bool> TestSmtpConfiguration(SmtpTestDto request);
    Task<bool> IsConfigured();
}