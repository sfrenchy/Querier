using DocumentFormat.OpenXml.Office2010.ExcelAc;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Querier.Api.Services.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Querier.Api.Models.Responses;

namespace Querier.Api.Controllers.UI
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UIPageController : ControllerBase
    {
        private readonly ILogger<UIPageController> _logger;
        private readonly IUIPageService _uiPageService;

        /// <summary>
        /// Constructor
        /// </summary>
        public UIPageController(ILogger<UIPageController> logger, IUIPageService uiPageService)
        {
            _logger = logger;
            _uiPageService = uiPageService;
        }

        [HttpGet("Index")]
        public ActionResult Index()
        {
            return new OkObjectResult(_uiPageService.Index());
        }

        [HttpGet]
        [Route("GetPage/{pageId}")]
        public async Task<IActionResult> GetPageAsync(int? pageId)
        {
            return new OkObjectResult(await _uiPageService.GetPageAsync(pageId));
        }

        /// <summary>
        /// Used to get all pages
        /// </summary>
        /// <param name="request">The add category request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpGet("GetPages")]
        public async Task<IActionResult> GetPagesAsync()
        {
            return new OkObjectResult(await _uiPageService.GetPagesAsync());
        }

        /// <summary>
        /// Used to get all pages with format for datatable
        /// </summary>
        /// <param name="request">The add category request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("GetAllPagesDatatable")]
        public async Task<IActionResult> GetAllPagesDatatableAsync([FromBody] ServerSideRequest datatableRequest)
        {
            return new OkObjectResult(await _uiPageService.GetAllPagesDatatableAsync(datatableRequest));
        }

        /// <summary>
        /// Used to create a new page
        /// </summary>
        /// <param name="model">The add page request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("AddPage")]
        public async Task<IActionResult> AddPageAsync([FromBody] AddPageRequest model)
        {
            return new OkObjectResult(await _uiPageService.AddPageAsync(model));
        }

        /// <summary>
        /// Used to delete a page
        /// </summary>
        /// <param name="model">The delete page request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpDelete("DeletePage")]
        public async Task<IActionResult> DeletePageAsync([FromBody] RemovePageRequest model)
        {
            QPage page = await _uiPageService.DeletePageAsync(model.PageId);

            if (page == null)
                return NotFound("Unable to find the page!");

            return new OkObjectResult(await _uiPageService.GetPagesAsync());
        }

        /// <summary>
        /// Used to edit a page
        /// </summary>
        /// <param name="model">The edot page request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPut("EditPage")]
        public async Task<IActionResult> EditPageAsync([FromBody] EditPageRequest model)
        {
            QPage updatePage = await _uiPageService.EditPageAsync(model); 

            if (updatePage == null)
                return NotFound("Unable to find the page!");

            return new OkObjectResult(await _uiPageService.GetPagesAsync());
        }

        /// <summary>
        /// Used to duplicate a page
        /// </summary>
        /// <param name="request">The duplicate page request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("DuplicatePage")]
        public async Task<IActionResult> DuplicatePageAsync([FromBody] DuplicatePageRequest model)
        {
            QPage duplicatePageSource = await _uiPageService.DuplicatePageAsync(model.PageId);

            if (duplicatePageSource == null)
                return NotFound("Unable to find the page!");

            return new OkObjectResult(await _uiPageService.GetPagesAsync());
        }

        [HttpPost("ExportPage")]
        public async Task<IActionResult> ExportPage([FromBody] DuplicatePageRequest model)
        {
            return new OkObjectResult(await _uiPageService.ExportPage(model.PageId));
        }

        [HttpPost("ExportPageConfiguration")]
        public async Task<IActionResult> ExportPageConfigurationAsync([FromBody] ExportPageRequest model)
        {
            return new OkObjectResult(await _uiPageService.ExportPageConfigurationAsync(model));
        }

        [HttpPost("ImportPageConfiguration")]
        public async Task<IActionResult> ImportPageConfigurationAsync()
        {
            if (Request.Form.Files != null)
            {
                if (Request.Form.Files.Count == 1)
                {
                    string tempPath = Path.GetTempFileName();
                    using (var requestFileStream = Request.Form.Files[0].OpenReadStream())
                    using (var stream = System.IO.File.Create(tempPath))
                    {
                        await requestFileStream.CopyToAsync(stream);
                    }

                    ExportPageResponse r = await _uiPageService.ImportPageConfigurationAsync(
                        new PageImportConfigRequest()
                        {
                            FilePath = tempPath,
                            CategoryId = int.Parse(Request.Form["categoryId"].ToString())
                        });
                    return new OkObjectResult(r);
                }
                throw new FileLoadException("Only one file is allowed when importing entities");
            }
            throw new FileLoadException("No file attached");
        }
    }
}