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
            try
            {
                logger.LogDebug("Checking if email service is configured");
                var isConfigured = await settings.GetSettingValueAsync("api:isConfigured", false);
                logger.LogInformation("Email service configuration status: {IsConfigured}", isConfigured);
                return isConfigured;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking email service configuration");
                return false;
            }
        }

        public async Task<bool> TestSmtpConfiguration(SmtpTestDto request)
        {
            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                logger.LogInformation("Testing SMTP configuration for host: {Host}:{Port}", request.Host, request.Port);

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    request.Host,
                    request.Port,
                    request.UseSSL ? SecureSocketOptions.StartTls : SecureSocketOptions.None
                );

                if (request.RequireAuth)
                {
                    logger.LogDebug("Attempting SMTP authentication");
                    await client.AuthenticateAsync(request.Username, request.Password);
                    logger.LogInformation("SMTP authentication successful");
                }

                await client.DisconnectAsync(true);
                logger.LogInformation("SMTP configuration test successful");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SMTP configuration test failed for host: {Host}:{Port}", 
                    request?.Host, request?.Port);
                throw;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                if (string.IsNullOrEmpty(to))
                {
                    throw new ArgumentException("Recipient email is required", nameof(to));
                }

                logger.LogInformation("Preparing to send email to: {To}", to);

                var smtpHost = await settings.GetSettingValueAsync<string>("smtp:host");
                var smtpPort = await settings.GetSettingValueAsync("smtp:port", 587);
                var smtpUsername = await settings.GetSettingValueAsync<string>("smtp:username");
                var smtpPassword = await settings.GetSettingValueAsync<string>("smtp:password");
                var mailFrom = await settings.GetSettingValueAsync<string>("smtp:senderEmail");
                var useSsl = await settings.GetSettingValueAsync("smtp:useSSL", true);
                var requiresAuth = await settings.GetSettingValueAsync("smtp:requiresAuth", false);

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(mailFrom))
                {
                    logger.LogError("SMTP configuration is incomplete");
                    return false;
                }

                using var client = new SmtpClient();
                logger.LogDebug("Connecting to SMTP server: {Host}:{Port}", smtpHost, smtpPort);
                
                await client.ConnectAsync(
                    smtpHost, 
                    smtpPort, 
                    useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None
                );

                if (requiresAuth)
                {
                    logger.LogDebug("Authenticating with SMTP server");
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

                logger.LogDebug("Sending email to {To}", to);
                await client.SendAsync(email);
                
                logger.LogDebug("Disconnecting from SMTP server");
                await client.DisconnectAsync(true);

                logger.LogInformation("Successfully sent email to: {To}", to);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email to: {To}", to);
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
                if (string.IsNullOrEmpty(to))
                {
                    throw new ArgumentException("Recipient email is required", nameof(to));
                }

                if (string.IsNullOrEmpty(templateName))
                {
                    throw new ArgumentException("Template name is required", nameof(templateName));
                }

                logger.LogInformation("Preparing to send templated email '{Template}' to: {To}", templateName, to);

                var template = await emailTemplateService.GetTemplateAsync(templateName, language, parameters);
                if (template == null)
                {
                    logger.LogError("Email template {Template} not found for language {Language}", 
                        templateName, language);
                    return false;
                }

                var result = await SendEmailAsync(to, subject, template, true);
                if (result)
                {
                    logger.LogInformation("Successfully sent templated email '{Template}' to: {To}", 
                        templateName, to);
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send templated email '{Template}' to: {To}", templateName, to);
                return false;
            }
        }
    }
}