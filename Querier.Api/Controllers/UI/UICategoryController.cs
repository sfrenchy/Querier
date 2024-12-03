using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Querier.Api.Services.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Querier.Api.Controllers.UI
{
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UICategoryController : ControllerBase
    {
        private readonly ILogger<UICategoryController> _logger;
        private readonly IUICategoryService _uiCategoryService;

        /// <summary>
        /// Constructor
        /// </summary>
        public UICategoryController(ILogger<UICategoryController> logger, IUICategoryService uiCategoryService)
        {
            _logger = logger;
            _uiCategoryService = uiCategoryService;
        }

        /// <summary>
        /// Used to get one category
        /// </summary>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpGet("GetCategory/{categoryId}")]
        public async Task<IActionResult> GetCategoryAsync(int categoryId)
        {
            return new OkObjectResult(await _uiCategoryService.GetCategoryAsync(categoryId));
        }

        /// <summary>
        /// Used to get all categories
        /// </summary>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategoriesAsync()
        {
            return new OkObjectResult(await _uiCategoryService.GetCategoriesAsync());
        }

        /// <summary>
        /// Used to create a new category
        /// </summary>
        /// <param name="request">The add category request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPost("AddCategory")]
        public async Task<IActionResult> AddCategoryAsync([FromBody] AddCategoryRequest request)
        {
            return new OkObjectResult(await _uiCategoryService.AddCategoryAsync(request));
        }

        /// <summary>
        /// Used to update a category
        /// </summary>
        /// <param name="request">The update category request</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpPut("UpdateCategory")]
        public async Task<IActionResult> UpdateCategoryAsync([FromBody] UpdateCategoryRequest request)
        {
            QPageCategory category = await _uiCategoryService.GetCategoryAsync(request.Id);
            if (category == null)
                return NotFound("Unable to find the category!");

            return new OkObjectResult(await _uiCategoryService.UpdateCategoryAsync(request));
        }

        /// <summary>
        /// Used to delete a category
        /// </summary>
        /// <param name="id">The category id to delete</param>
        /// <returns>Return a ObjectResult which holds status code (Ok:200/BadRequest:400/NotFound:404) and data</returns>
        [HttpDelete("DeleteCategory/{id}")]
        public async Task<IActionResult> DeleteCategoryAsync(int id)
        {
            QPageCategory category = await _uiCategoryService.GetCategoryAsync(id);
            if (category == null)
                return NotFound("Unable to find the category!");

            if (category.QPages.Count > 0)
                return BadRequest("The category can't be deleted, there are still page(s) for this!");

            return new OkObjectResult(await _uiCategoryService.DeleteCategoryAsync(category));
        }
    }
}
