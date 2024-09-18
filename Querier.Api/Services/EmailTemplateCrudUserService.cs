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
        Task<bool> AddUserEmailTemplate(QAddUserEmailTemplateRequest request);
    }
    public class EmailTemplateCrudUserService : IEmailTemplateCrudUserService
    {
        private readonly Models.Interfaces.IQUploadService _uploadService;
        public EmailTemplateCrudUserService(Models.Interfaces.IQUploadService uploadService)
        {
            _uploadService = uploadService;
        }


        public async Task<bool> AddUserEmailTemplate(QAddUserEmailTemplateRequest request)
        {
            //create stream from the string
            byte[] ContentBytes = Encoding.Default.GetBytes(request.ContentEmailTemplate);

            //Upload new file 
            HAUploadDefinitionFromApi requestPram = new HAUploadDefinitionFromApi()
            {
                Definition = new SimpleUploadDefinition()
                {
                    FileName = request.NameEmailTemplate,
                    Nature = QUploadNatureEnum.UserEmail
                },
                UploadStream = new MemoryStream(ContentBytes)
            };

            var IdUpload = await _uploadService.UploadFileFromApiAsync(requestPram);
            return true;
        }

    }
}
