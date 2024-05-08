using Querier.Api.Models.Requests;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ImportController : ControllerBase
    {
        private readonly ILogger<ImportController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IImportService _importService;
        public ImportController(IImportService importService, IConfiguration configuration, ILogger<ImportController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _importService = importService;
        }

        [HttpPost("AskImportFromContextEntitiesFromCSV")]
        public async Task AskImportFromContextEntitiesFromCSVAsync([FromForm] ImportEntitiesFromCSVRequest request)
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
                    _importService.AskImportEntitiesFromCSV(new ImportEntitiesFromCSVRequest() {
                        allowUpdate = Request.Form["allowUpdate"] == "true",
                        filePath = tempPath,
                        identifierColumn = Request.Form["identifierColumn"],
                        requestUserEmail = Request.Form["requestUserEmail"],
                        contextType = Request.Form["contextType"],
                        entityType = Request.Form["entityType"]
                    });
                }
                else
                {
                    throw new FileLoadException("Only one file is allowed when importing entities");
                }
            }
        }
    }
}