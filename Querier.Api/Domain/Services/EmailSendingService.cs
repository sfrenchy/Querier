using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MailKit.Security;
using MimeKit;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Querier.Api.Domain.Services
{
    public class SmtpEmailSendingService(
        ILogger<SmtpEmailSendingService> logger,
        ISettingService settings,
        IEmailTemplateService emailTemplateService)
        : IEmailSendingService
    {
        public async Task<bool> IsConfigured()
        {
            return bool.Parse(await settings.GetSettingValue("api:isConfigured", "false"));
        }

        public async Task<bool> TestSmtpConfiguration(SmtpTestDto request)
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
                logger.LogError(ex, "SMTP test failed");
                throw;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                var smtpHost = await settings.GetSettingValue("smtp:host");
                var smtpPort = int.Parse(await settings.GetSettingValue("smtp:port", "587"));
                var smtpUsername = await settings.GetSettingValue("smtp:username");
                var smtpPassword = await settings.GetSettingValue("smtp:password");
                var mailFrom = await settings.GetSettingValue("smtp:senderEmail");
                var useSsl = bool.Parse(await settings.GetSettingValue("smtp:useSSL", "true"));
                var requiresAuth = bool.Parse(await settings.GetSettingValue("smtp:requiresAuth", "false"));

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
                logger.LogError(ex, "Failed to send email");
                return false;
            }
        }

        public async Task<bool> SendTemplatedEmailAsync(string to, string subject, string templateName, string language, Dictionary<string, string> parameters)
        {
            try
            {
                var template = await emailTemplateService.GetTemplateAsync(templateName, language, parameters);
                if (template == null)
                {
                    logger.LogError($"Email template {templateName} not found for language {language}");
                    return false;
                }

                return await SendEmailAsync(to, subject, template, true);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send templated email");
                return false;
            }
        }
    }
}