using Querier.Api.Models;
using Querier.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Querier.Api.Models.Responses;
using Querier.Api.Models.Cards;
using Microsoft.AspNetCore.Authorization;
using Querier.Api.Models.Common;
using System.Collections.Generic;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Requests;
using System.Security.Claims;
using Querier.Api.Models.Enums;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTemplateCrudAppController : ControllerBase
    {
        private IEmailTemplateCrudCommonService _emailTemplateCrudCommonService;
        public EmailTemplateCrudAppController(IEmailTemplateCrudCommonService emailTemplateCrudCommonService)
        {
            _emailTemplateCrudCommonService = emailTemplateCrudCommonService;
        }
           
        [HttpPost("GetAllAppEmailTemplate")]
        public IActionResult GetAllAppEmailTemplate([FromBody] ServerSideRequest datatableRequest)
        {
            return new OkObjectResult(_emailTemplateCrudCommonService.GetAllEmailTemplates(datatableRequest, QUploadNatureEnum.ApplicationEmail));
        }

        [HttpGet("GetContentEmailTemplateApp/{TemplateId}")]
        public async Task<IActionResult> GetContentEmailTemplateApp(int TemplateId)
        {
            return new OkObjectResult( await _emailTemplateCrudCommonService.GetContentEmailTemplate(TemplateId) );
        }

        [HttpPut("UpdateContentEmailTemplateApp")]
        public async Task<IActionResult> UpdateContentEmailTemplateApp([FromBody] QUpdateEmailTemplateRequest request)
        {
            return new OkObjectResult(await _emailTemplateCrudCommonService.UpdateContentEmailTemplate(request, QUploadNatureEnum.ApplicationEmail));
        }

        [HttpGet("GetDescriptionVariablesTemplates")]
        public IActionResult GetDescriptionVariablesTemplates()
        {
            return new OkObjectResult(_emailTemplateCrudCommonService.GetDescriptionVariablesTemplates());
        }

        [HttpPost("SendEmailForTestTemplateUser")]
        public async Task<IActionResult> SendEmailForTestTemplateUser([FromBody] QSendEmailForTestTemplateRequest request)
        {
            return new OkObjectResult(await _emailTemplateCrudCommonService.SendEmailForTestTemplate(request, HttpContext.User.FindFirst(ClaimTypes.Email).Value));
        }
    }
}
