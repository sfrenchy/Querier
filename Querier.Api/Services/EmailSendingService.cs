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

namespace Querier.Api.Services
{
    public interface IEmailSendingService
    {
        Task<dynamic> SendEmailAsync(SendMailParamObject ObjectRequest, List<AttachmentTypeProperty> attachments = null);// type return : SendEmailResult
    }
    public class SMTPEmailSendingService : IEmailSendingService
    {
        private readonly ILogger<SMTPEmailSendingService> _logger;
        private readonly IConfiguration _configuration;

        public SMTPEmailSendingService(ILogger<SMTPEmailSendingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<dynamic> SendEmailAsync(SendMailParamObject ObjectRequest, List<AttachmentTypeProperty> attachments = null)
        {
            //variable come from appSettings
            var mailCredential = _configuration.GetSection("ApplicationSettings:SMTP:SmtpCredentialMail").Get<string>();
            var PasswordCredential = _configuration.GetSection("ApplicationSettings:SMTP:SmtpCredentialPassword").Get<string>();
            var defaultCredentialSmtp = _configuration.GetSection("ApplicationSettings:SMTP:SmtpUseDefaultCredentials").Get<bool>();
            var hostSmtp = _configuration.GetSection("ApplicationSettings:SMTP:SmtpHost").Get<string>();
            var PortSmtp = _configuration.GetSection("ApplicationSettings:SMTP:SmtpPort").Get<int>();
            var sslSmtp = _configuration.GetSection("ApplicationSettings:SMTP:SmtpEnableSsl").Get<bool>();
            
            string Content = ObjectRequest.bodyEmail;
            if (ObjectRequest.bodyHtmlEmail == true)
            {
                //fill body content
                //! warning the propertys of ObjectRequest.VariablesFillBodyContent need to be same with key $xxx$ fill in body html content
                var template = new Template(ObjectRequest.bodyEmail, '$', '$');
                ParametersEmail parameters = ObjectRequest.ParameterEmailToFillContent;
                template.Add("param", parameters);
                Content = template.Render();
            }
            
            using (var message = new MimeMessage())
            {
                message.From.Add(new MailboxAddress(ObjectRequest.EmailFrom, ObjectRequest.EmailFrom));
                ObjectRequest.EmailTo.Split(';').ToList().ForEach(m => message.To.Add(new MailboxAddress(m, m)));
                
                message.Subject = ObjectRequest.SubjectEmail;
                var bodyBuilder = new BodyBuilder
                {
                    TextBody = Content,
                    HtmlBody = Content
                };
                
                
                if (attachments != null && attachments.Count != 0)
                {
                    foreach (var attachement in attachments)
                    {
                        bodyBuilder.Attachments.Add(attachement.fileName, attachement.contentStream, ContentType.Parse(attachement.contentType));
                    }
                }
                
                message.Body = bodyBuilder.ToMessageBody();
                
                string responseMessage;
                dynamic response;
                try
                {
                    using (var client = new SmtpClient())
                    {
                        if (sslSmtp)
                        {
                            // SecureSocketOptions.StartTls force a secure connection over TLS
                            await client.ConnectAsync(hostSmtp, PortSmtp, SecureSocketOptions.StartTls);
                        }
                        else
                        {
                            await client.ConnectAsync(hostSmtp, PortSmtp);
                        }
                        
                        if (defaultCredentialSmtp)
                        {
                            await client.AuthenticateAsync(
                                userName: mailCredential, // the userName is the exact string "apikey" and not the API key itself.
                                password: PasswordCredential // password is the API key
                            );
                        }

                        await client.SendAsync(message);
                        await client.DisconnectAsync(true);
                    }
                    responseMessage = "Email was send, it's fine";
                    response = new { success = true, message = responseMessage };
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    responseMessage = ex.ToString();
                    response = new { success = false, message = responseMessage };
                }
                finally
                {
                    //close the stream:
                    if (attachments != null && attachments.Count != 0)
                    {
                        for (int i = 0; i < attachments.Count; i++)
                        {
                            var attachObject = attachments[i];
                            attachObject.contentStream.Close();
                        }
                    }
                }
                return response;
            }
        }
    }
}