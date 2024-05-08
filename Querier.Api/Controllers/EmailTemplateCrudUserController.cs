using Querier.Api.Models.Common;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Requests;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Reporting.Map.WebForms.BingMaps;
using System.Security.Claims;
using System.Threading.Tasks;
using Querier.Api.Models.Interfaces;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTemplateCrudUserController : ControllerBase
    {
        private IEmailTemplateCrudUserService _emailTemplateCrudUserService;
        private IHAUploadService _uploadService;
        private IEmailTemplateCrudCommonService _emailTemplateCrudCommonService;
        public EmailTemplateCrudUserController(IEmailTemplateCrudUserService emailTemplateCrudUserService, IHAUploadService uploadService, IEmailTemplateCrudCommonService emailTemplateCrudCommonService)
        {
            _emailTemplateCrudUserService = emailTemplateCrudUserService;
            _uploadService = uploadService;
            _emailTemplateCrudCommonService = emailTemplateCrudCommonService;
        }

        [HttpPost("GetAllUserEmailTemplate")]
        public IActionResult GetAllUserEmailTemplate([FromBody] ServerSideRequest datatableRequest)
        {
            return new  OkObjectResult(_emailTemplateCrudCommonService.GetAllEmailTemplates(datatableRequest, HAUploadNatureEnum.UserEmail));
        }

        [HttpDelete("DeletUserEmailTemplate/{TemplateId}")]
        public async Task<IActionResult> DeletUserEmailTemplate(int TemplateId)
        {
            return new OkObjectResult(await _uploadService.DeleteUploadAsync(TemplateId));
        }

        [HttpPost("AddUserEmailTemplate")]
        public async Task<IActionResult> AddUserEmailTemplate(HAAddUserEmailTemplateRequest request)
        {
            return new OkObjectResult(await _emailTemplateCrudUserService.AddUserEmailTemplate(request));
        }

        [HttpPut("UpdateContentEmailTemplateUser")]
        public async Task<IActionResult> UpdateContentEmailTemplateUser(HAUpdateEmailTemplateRequest request)
        {
            return new OkObjectResult(await _emailTemplateCrudCommonService.UpdateContentEmailTemplate(request, HAUploadNatureEnum.UserEmail));
        }

        [HttpGet("GetContentEmailTemplateUser/{TemplateId}")]
        public async Task<IActionResult> GetContentEmailTemplateUser(int TemplateId)
        {
            return new OkObjectResult(await _emailTemplateCrudCommonService.GetContentEmailTemplate(TemplateId));
        }

        [HttpPost("SendEmailForTestTemplateUser")]
        public async Task<IActionResult> SendEmailForTestTemplateUser([FromBody] HASendEmailForTestTemplateRequest request)
        {
            return new OkObjectResult(await _emailTemplateCrudCommonService.SendEmailForTestTemplate(request, HttpContext.User.FindFirst(ClaimTypes.Email).Value));
        }

        [HttpGet("GetDescriptionVariablesTemplates")]
        public IActionResult GetDescriptionVariablesTemplates()
        {
            return new OkObjectResult(_emailTemplateCrudCommonService.GetDescriptionVariablesTemplates());
        }
    }
}
