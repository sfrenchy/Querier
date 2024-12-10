using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Antlr4.StringTemplate;
using Querier.Api.Models;
using Querier.Api.Models.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;
using System.Net.Mail;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Querier.Api.Services
{
    public interface IEmailSendingService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, string language, Dictionary<string, string> parameters);
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

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                var smtpHost = await _settings.GetSettingValue("api:smtp:host");
                var smtpPort = int.Parse(await _settings.GetSettingValue("api:smtp:port", "587"));
                var smtpUsername = await _settings.GetSettingValue("api:smtp:username");
                var smtpPassword = await _settings.GetSettingValue("api:smtp:password");
                var mailFrom = await _settings.GetSettingValue("api:smtp:senderEmail");
                var useSsl = bool.Parse(await _settings.GetSettingValue("api:smtp:useSSL", "true"));
                var requiresAuth = bool.Parse(await _settings.GetSettingValue("api:smtp:requiresAuth", "false"));

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                if (requiresAuth)
                {
                    await client.AuthenticateAsync(smtpUsername, smtpPassword);
                }

                var message = new MimeMessage
                {
                    From = { new MailboxAddress("", mailFrom) },
                    To = { new MailboxAddress("", to) },
                    Subject = subject,
                    Body = new TextPart(isHtml ? "html" : "plain") { Text = body }
                };

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {to}");
                return false;
            }
        }

        public async Task<bool> SendTemplatedEmailAsync(
            string to, 
            string subject, 
            string templateName, 
            string language,
            Dictionary<string, string> parameters)
        {
            try
            {
                var body = await _emailTemplateService.GetTemplateAsync(templateName, language, parameters);
                return await SendEmailAsync(to, subject, body, isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending templated email to {to}");
                return false;
            }
        }
    }
}