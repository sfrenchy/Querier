using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Requests;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Querier.Api.Models.Interfaces;

namespace Querier.Api.Services
{
    public interface IEmailTemplateCrudUserService
    {
        Task<bool> AddUserEmailTemplate(HAAddUserEmailTemplateRequest request);
    }
    public class EmailTemplateCrudUserService : IEmailTemplateCrudUserService
    {
        private readonly IHAUploadService _uploadService;
        public EmailTemplateCrudUserService(IHAUploadService uploadService)
        {
            _uploadService = uploadService;
        }


        public async Task<bool> AddUserEmailTemplate(HAAddUserEmailTemplateRequest request)
        {
            //create stream from the string
            byte[] ContentBytes = Encoding.Default.GetBytes(request.ContentEmailTemplate);

            //Upload new file 
            HAUploadDefinitionFromApi requestPram = new HAUploadDefinitionFromApi()
            {
                Definition = new SimpleUploadDefinition()
                {
                    FileName = request.NameEmailTemplate,
                    Nature = HAUploadNatureEnum.UserEmail
                },
                UploadStream = new MemoryStream(ContentBytes)
            };

            var IdUpload = await _uploadService.UploadFileFromApiAsync(requestPram);
            return true;
        }

    }
}
