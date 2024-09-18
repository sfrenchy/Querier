using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Compression;
using System.Security.Policy;
using System.Threading.Tasks;
using Querier.Api.Models.Interfaces;
using IQUploadService = Querier.Api.Models.Interfaces.IQUploadService;

namespace Querier.Api.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class QUploadController : ControllerBase
    {
        private readonly ILogger _logger;
        private IQUploadService _uploadService;
        private readonly IWebHostEnvironment _environment;
        public QUploadController(ILogger<QUploadController> logger, IQUploadService uploadService, IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _uploadService = uploadService;
            _environment = hostEnvironment;
        }
        
        [HttpPost]
        [Route("UploadFromVM")]
        public async Task<IActionResult> UploadFromVMAsync([FromForm] HAUploadDefinitionVM upload)
        {
            return Ok(await _uploadService.UploadFileFromVMAsync(upload));
        }

        [HttpPost]
        [Route("UploadFromApi")]
        public async Task<IActionResult> UploadFromApiAsync([FromForm] HAUploadDefinitionFromApi upload)
        {
            return Ok(await _uploadService.UploadFileFromApiAsync(upload));
        }

        [HttpGet]
        [Route("GetFile/{id}")]
        public async Task<IActionResult> GetFileAsync(int id)
        {
            QUploadDefinition upload = await _uploadService.GetFileAsync(id);

            using (var stream = new FileStream(upload.Path, FileMode.Open))
            {
                var fileName = Path.GetFileName(upload.Path);
                var file = new FormFile(stream, 0, stream.Length, fileName, fileName);
                file.Headers = new HeaderDictionary();
                file.ContentType = upload.MimeType;
                file.ContentDisposition = "attachment";

                return File(System.IO.File.ReadAllBytes(upload.Path), file.ContentType, upload.FileName);
            }
        }

        [HttpDelete]
        [Route("DeleteFile")]
        public async Task<IActionResult> DeleteFileAsync([FromBody] RemoveFileRequest model)
        {
            if(await _uploadService.DeleteUploadAsync(model.UploadId))
                return Ok("The upload has been perfectly deleted");
            else
                return Problem("An error occured during the supression");
        }

        [HttpDelete("DeleteFromRules")]
        public async Task<IActionResult> DeleteFromRulesAsync()
        {
            if (await _uploadService.DeleteFromRules())
                return Ok("the treatment has been completed");
            else
                return Problem("An error occurred while deleting some uploads");
        }

        [HttpGet]
        [Route("GetAllFiles")]
        public async Task<IActionResult> GetAllFilesAsync()
        {
            return Ok(await _uploadService.GetUploadListAsync());
        }

        [HttpGet]
        [Route("ExportZip")]
        public async Task<IActionResult> ExportZipAsync()
        {
            string zipFilePath = await _uploadService.CompressFilesAsync();
            var fs = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose);
            return File(
                fileStream: fs,
                contentType: System.Net.Mime.MediaTypeNames.Application.Octet,
                fileDownloadName: Path.GetFileName(zipFilePath));
        }

        [HttpPost]
        [Route("ImportZip")]
        public async Task<IActionResult> ImportZipAsync([FromForm] UploadBackUpRequest file)
        {
            await _uploadService.UploadBackUpAsync(file);
            return Ok();
        }
    }
}