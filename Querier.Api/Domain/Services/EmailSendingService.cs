using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MailKit.Security;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using Querier.Api.Application.DTOs.Requests.Smtp;

namespace Querier.Api.Domain.Services
{
    public interface IEmailSendingService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, string language, Dictionary<string, string> parameters);
        Task<bool> TestSmtpConfiguration(SmtpTestRequest request);
        Task<bool> IsConfigured();
    }

    public class SMTPEmailSendingService : IEmailSendingService
    {
        private readonly ILogger<SMTPEmailSendingService> _logger;
        private readonly ISettingService _settings;
        private readonly IEmailTemplateService _emailTemplateService;

        public SMTPEmailSendingService(
            ILogger<SMTPEmailSendingService> logger,
            ISettingService settings,
            IEmailTemplateService emailTemplateService)
        {
            _logger = logger;
            _settings = settings;
            _emailTemplateService = emailTemplateService;
        }

        public async Task<bool> IsConfigured()
        {
            return bool.Parse(await _settings.GetSettingValue("api:isConfigured", "false"));
        }

        public async Task<bool> TestSmtpConfiguration(SmtpTestRequest request)
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(
                    request.Host,
                    request.Port,
                    request.UseSSL ? SecureSocketOptions.StartTls : SecureSocketOptions.None
                );

                if (request.RequireAuth)
                {
                    await client.AuthenticateAsync(request.Username, request.Password);
                }

                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP test failed");
                throw;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                var smtpHost = await _settings.GetSettingValue("smtp:host");
                var smtpPort = int.Parse(await _settings.GetSettingValue("smtp:port", "587"));
                var smtpUsername = await _settings.GetSettingValue("smtp:username");
                var smtpPassword = await _settings.GetSettingValue("smtp:password");
                var mailFrom = await _settings.GetSettingValue("smtp:senderEmail");
                var useSsl = bool.Parse(await _settings.GetSettingValue("smtp:useSSL", "true"));
                var requiresAuth = bool.Parse(await _settings.GetSettingValue("smtp:requiresAuth", "false"));

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                if (requiresAuth)
                {
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("", mailFrom));
                email.To.Add(new MailboxAddress("", to));
                email.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                email.Body = bodyBuilder.ToMessageBody();

                await client.SendAsync(email);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email");
                return false;
            }
        }

        public async Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, string language, Dictionary<string, string> parameters)
        {
            try
            {
                var template = await _emailTemplateService.GetTemplateAsync(templateName, language, parameters);
                if (template == null)
                {
                    _logger.LogError($"Email template {templateName} not found for language {language}");
                    return false;
                }

                return await SendEmailAsync(to, subject, template, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send templated email");
                return false;
            }
        }
    }
}