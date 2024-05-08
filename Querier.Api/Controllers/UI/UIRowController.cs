using Querier.Api.Models;
using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Castle.Components.DictionaryAdapter;
using Querier.Api.Services.UI;
using System.Collections.Generic;

namespace Querier.Api.Controllers.UI
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UIRowController : ControllerBase
    {
        private readonly ILogger<UIRowController> _logger;
        private readonly IUIRowService _uiRowService;

        /// <summary>
        /// Constructor
        /// </summary>
        public UIRowController(ILogger<UIRowController> logger, IUIRowService uiRowService)
        {
            _logger = logger;
            _uiRowService = uiRowService;
        }

        /// <summary>
        /// Used to get row(s) from a page
        /// </summary>
        /// <param name="pageId">The id of the page</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpGet("GetRows/{pageId}")]
        public async Task<IActionResult> GetRowsAsync(int pageId)
        {
            return new OkObjectResult(await _uiRowService.GetRowsAsync(pageId));
        }

        /// <summary>
        /// Used to create a new row
        /// </summary>
        /// <param name="addRowRequest">The add row request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("AddRow")]
        public async Task<IActionResult> AddRowAsync([FromBody] AddRowRequest addRowRequest)
        {
            HAPage page = await _uiRowService.AddRowAsync(addRowRequest.PageId);

            if (page == null)
                return NotFound("Unable to find the page!");

            return new OkObjectResult(page.HAPageRows);
        }

        /// <summary>
        /// Used to delete a row
        /// </summary>
        /// <param name="rowId">The row id to delete</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpDelete("DeleteRow/{rowId}")]
        public async Task<IActionResult> DeleteRowAsync(int rowId)
        {
            HAPageRowVM row = await _uiRowService.DeleteRowAsync(rowId); 

            if (row == null)
                return NotFound("Unable to find the row!");

            return new OkObjectResult(await _uiRowService.GetRowsAsync(row.HAPageId));
        }

        /// <summary>
        /// Used to update the order of a row
        /// </summary>
        /// <param name="page">the page to update the order of its rows</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPut("UpdateRowOrder")]
        public async Task<IActionResult> UpdateRowOrderAsync([FromBody] HAPageVM page)
        {
            if (page == null)
                return NotFound("Unable to find the page!");

            List<HAPageRowVM> rows = await _uiRowService.UpdateRowOrder(page);
            return new OkObjectResult(rows);
        }
    }
}
