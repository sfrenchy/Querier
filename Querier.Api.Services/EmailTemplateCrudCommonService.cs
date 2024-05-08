using System.Text;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Email;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests;
using Querier.Api.Models.Responses;
using Querier.Tools;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Querier.Api.Services
{
    public interface IEmailTemplateCrudCommonService
    {
        public Task<HAEmailTemplateManagerResponse> GetContentEmailTemplate(int templateId);
        public Task<bool> UpdateContentEmailTemplate(HAUpdateEmailTemplateRequest request, HAUploadNatureEnum emailNature);
        Dictionary<string, string> GetDescriptionVariablesTemplates();
        public Task<dynamic> SendEmailForTestTemplate(HASendEmailForTestTemplateRequest request, string emailTo);
        public ServerSideResponse<HAUploadDefinition> GetAllEmailTemplates(ServerSideRequest datatableRequest, HAUploadNatureEnum emailNature);
    }

    public class EmailTemplateCrudCommonService : IEmailTemplateCrudCommonService
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IHAUploadService _uploadService;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApiUser> _userManager;
        private readonly IEmailSendingService _emailSending;
        public EmailTemplateCrudCommonService(IDbContextFactory<ApiDbContext> contextFactory, IHAUploadService uploadService, IConfiguration configuration, UserManager<ApiUser> userManager, IEmailSendingService emailSending)
        {
            _contextFactory = contextFactory;
            _uploadService = uploadService;
            _configuration = configuration;
            _userManager = userManager;
            _emailSending = emailSending;
        }

        public async Task<HAEmailTemplateManagerResponse> GetContentEmailTemplate(int templateId)
        {
            Stream fileStream = await _uploadService.GetUploadStream(templateId);
            byte[] byteArrayFile;
            using (MemoryStream ms = new MemoryStream())
            {
                fileStream.CopyTo(ms);
                byteArrayFile = ms.ToArray();
            }
            string HtmlcontentString = System.Text.Encoding.UTF8.GetString(byteArrayFile);

            HAEmailTemplateManagerResponse response = new HAEmailTemplateManagerResponse()
            {
                Content = HtmlcontentString,
            };
            return response;
        }

        public async Task<bool> UpdateContentEmailTemplate(HAUpdateEmailTemplateRequest request, HAUploadNatureEnum emailNature)
        {
            //delete existed file 
            bool res = await _uploadService.DeleteUploadAsync(request.TemplateId);
            if (!res)
            {
                return false;
            }

            //create stream from the string
            byte[] ContentBytes = Encoding.Default.GetBytes(request.TemplateNewContent);

            //Upload new file 
            HAUploadDefinitionFromApi requestPram = new HAUploadDefinitionFromApi()
            {
                Definition = new SimpleUploadDefinition() {
                    FileName = request.TemplateName,
                    Nature = emailNature
                },
                UploadStream = new MemoryStream(ContentBytes)
            };

            var IdUpload = await _uploadService.UploadFileFromApiAsync(requestPram);
            return true;
        }

        public Dictionary<string, string> GetDescriptionVariablesTemplates()
        {
            Dictionary<string, string> ParametersEmail = new DescriptionVariable().Description;
            return ParametersEmail;
        }

        public async Task<dynamic> SendEmailForTestTemplate(HASendEmailForTestTemplateRequest request, string emailTo)
        {
            //Get user from mail:
            var user = await _userManager.FindByEmailAsync(emailTo);

            //Get the content string of the body Email with a stream:
            Stream fileStream = await _uploadService.GetUploadStream(request.IdEmailTemplate);
            byte[] byteArrayFile;
            using (MemoryStream ms = new MemoryStream())
            {
                fileStream.CopyTo(ms);
                byteArrayFile = ms.ToArray();
            }
            string bodyEmail = System.Text.Encoding.UTF8.GetString(byteArrayFile);
            string emailFrom = _configuration.GetSection("ApplicationSettings:SMTP:mailFrom").Get<string>();

            ParametersEmail ParamsEmail = new ParametersEmail(_configuration, null, user);

            //send mail
            SendMailParamObject mailObject = new SendMailParamObject()
            {
                EmailTo = user.Email,
                EmailFrom = emailFrom,
                bodyEmail = bodyEmail,
                SubjectEmail = "Test template",
                bodyHtmlEmail = true,
                CopyEmail = "",
                ParameterEmailToFillContent = ParamsEmail
            };
            dynamic response = await _emailSending.SendEmailAsync(mailObject);

            return response;
        }

        public ServerSideResponse<HAUploadDefinition> GetAllEmailTemplates(ServerSideRequest datatableRequest, HAUploadNatureEnum emailNature)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                ServerSideResponse<HAUploadDefinition> response = new ServerSideResponse<HAUploadDefinition>();
                var res = apidbContext.HAUploadDefinitions.Where(t => t.Nature == emailNature).DatatableFilter(datatableRequest, out int? filteredCount);

                response.sums = null;
                response.draw = datatableRequest.draw;
                response.data = res;
                response.recordsFiltered = (int)filteredCount;
                response.recordsTotal = res.Count;

                return response;
            }
        }
    }
}
