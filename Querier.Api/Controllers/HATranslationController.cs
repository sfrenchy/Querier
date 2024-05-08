using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Querier.Api.Services;
using Querier.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HATranslationController : ControllerBase
    {
        private readonly ILogger<HATranslationController> _logger;
        private readonly IDbContextFactory<ApiDbContext> _apiDbContextFactory;
        private IHATranslationService _translationService;

        public HATranslationController(ILogger<HATranslationController> logger, IHATranslationService translationService, IDbContextFactory<ApiDbContext> apiDbContextFactory)
        {
            _logger = logger;
            _translationService = translationService;
            _apiDbContextFactory = apiDbContextFactory;
        }

        [AllowAnonymous]
        [HttpGet("GetTranslations")]
        public IActionResult GetTranslations()
        {
            return Ok(_translationService.GetTranslations());
        }

        [AllowAnonymous]
        [HttpPost("GetTranslationTable")]
        [ProducesResponseType(typeof(ServerSideResponse<HATranslation>), 200)]
        public async Task<IActionResult> GetTranslationTable(ServerSideRequest request)
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                ServerSideResponse<HATranslation> response = new ServerSideResponse<HATranslation>();
                response.data = _translationService.GetTranslationTable().DatatableFilter(request, out int? countFiltered).ToList();
                response.draw = request.draw;
                response.recordsTotal = apiDbContext.HADBConnections.Count();
                response.recordsFiltered = (int)countFiltered;
                response.sums = new Dictionary<string, object>();
                return Ok(response);
            }
        }

        [AllowAnonymous]
        [HttpGet("GetSignature")]
        public IActionResult GetSignature()
        {
            return Ok(_translationService.GetSignature());
        }

        [HttpPost("UpdateTranslation")]
        public IActionResult UpdateTranslation([FromBody] HAUpdateTranslationRequest request)
        {
            _translationService.UpdateTranslation(request);
            return Ok();
        }

        [HttpPut("UpdateGlobalTranslation")]
        public IActionResult UpdateGlobalTranslation([FromBody] HAUpdateGlobalTranslationRequest request)
        {
            if (_translationService.UpdateGlobalTranslation(request))
                return Ok();
            else
                return Problem("An error occured during the update");
        }
    }
}
