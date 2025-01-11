using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit.Security;
using Microsoft.Extensions.Logging;
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
            return await settings.GetSettingValueAsync("api:isConfigured", false);
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
                var smtpHost = await settings.GetSettingValueAsync<string>("smtp:host");
                var smtpPort = await settings.GetSettingValueAsync("smtp:port", 587);
                var smtpUsername = await settings.GetSettingValueAsync<string>("smtp:username");
                var smtpPassword = await settings.GetSettingValueAsync<string>("smtp:password");
                var mailFrom = await settings.GetSettingValueAsync<string>("smtp:senderEmail");
                var useSsl = await settings.GetSettingValueAsync("smtp:useSSL", true);
                var requiresAuth = await settings.GetSettingValueAsync("smtp:requiresAuth", false);

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