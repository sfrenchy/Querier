using Querier.Api.Models;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EmailSendingController : ControllerBase
    {
        private readonly ILogger<EmailSendingController> _logger;
        private readonly IEmailSendingService _emailSendingService;

        public EmailSendingController(IEmailSendingService emailSendingService, ILogger<EmailSendingController> logger)
        {
            _logger = logger;
            _emailSendingService = emailSendingService;
        }

        /// <summary>
        /// Send mail via smtp protocol
        /// </summary>
        /// <param name="ObjectRequest">Object which contains informations for sending mail</param>
        /// <returns>return the result of the method, he can be the exception string or just message to inform the mail was send </returns>
        [HttpPost("SendEmailBySmtp")]
        public async Task<IActionResult> SendEmailBySmtp([FromForm] SendMailParamObject ObjectRequest) //List<AttachmentTypeProperty> attachments
        {
            //create the list of attachements from form:
            List<AttachmentTypeProperty> ListAttachment = new List<AttachmentTypeProperty>();
            if (Request.Form.Files != null)
            {
                foreach (IFormFile file in Request.Form.Files)
                {
                    if (file.Length > 0)
                    {
                        //create an attachObject and fill it 
                        Stream fileStream = file.OpenReadStream();
                        var attachObject = new AttachmentTypeProperty();
                        attachObject.contentStream = fileStream;
                        attachObject.fileName = file.FileName;

                        //find the content type of the file
                        const string DefaultContentType = "application/octet-stream";
                        var provider = new FileExtensionContentTypeProvider();
                        if (!provider.TryGetContentType(file.FileName, out string contentType))
                        {
                            contentType = DefaultContentType;
                        }
                        attachObject.contentType = contentType;
                        ListAttachment.Add(attachObject);
                    }
                }
            }
            var response = await _emailSendingService.SendEmailAsync(ObjectRequest, ListAttachment);
            return Ok(response);
        }

        [HttpPost("SendEmailBySmtpTest")]
        public async Task<IActionResult> SendEmailBySmtpTest(string from, string to) //List<AttachmentTypeProperty> attachments
        {
            SendMailParamObject ObjectRequest = new SendMailParamObject()
            {
                EmailFrom = from,
                EmailTo = to,
                bodyEmail = "Test email from herdiaApp",
                SubjectEmail = "This is a test from herdiaApp to check email settings. If you read this, it works",
                bodyHtmlEmail = false
            };
            return Ok(await _emailSendingService.SendEmailAsync(ObjectRequest));
        }
    }
}
