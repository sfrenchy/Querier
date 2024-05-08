using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Enums.Ged;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests.Ged;
using Querier.Api.Models.Responses.Ged;
using Querier.Api.Services.Factory;
using Querier.Api.Services.Ged;
using Querier.Tools;
using Microsoft.AspNetCore.Mvc;

namespace Querier.Api.Controllers.Ged
{
    [Route("api/[controller]")]
    [ApiController]
    public class GedController : ControllerBase
    {
        private readonly FileDepositFactory _fileDepositFactory;
        private readonly IFileDepositService _fileDepositService;

        public GedController(FileDepositFactory fileDepositFactory, IFileDepositService fileDepositService)
        {
            _fileDepositFactory = fileDepositFactory;
            _fileDepositService = fileDepositService;
        }

        [HttpGet("FillFileInformationByTag/{tag}")]
        public async Task<IActionResult> FillFileInformationByTag(string tag)
        {
            var instanceClass = _fileDepositFactory.CreateClassInstanceByTag(tag);
            if(instanceClass == null)
            {
                return Ok(new { error = "tag not find" });
            }
            return Ok(await instanceClass.FillFileInformations());
        }

        [HttpGet("FillFileInformationByType/{type}")]
        public async Task<IActionResult> FillFileInformationByType(TypeFileDepositEnum type)
        {
            IHAFileReadOnlyDeposit instanceClass = _fileDepositFactory.CreateClassInstanceByType(type);

            return Ok(await instanceClass.FillFileInformations());
        }

        [HttpPost("GetSpecificInformationByTag/{tag}")]
        public async Task<IActionResult> GetSpecificInformationByTag([FromBody] List<GetSpecificInformationRequest> variablesFilter, string tag)
        {
           var instanceClass = _fileDepositFactory.CreateClassInstanceByTag(tag);
            if (instanceClass == null)
            {
                return Ok(new { error = "tag not find" });
            }
            return Ok(await instanceClass.GetSpecificInformation(variablesFilter));
        }

        [HttpPost("GetSpecificInformationByType/{type}")]
        public async Task<IActionResult> GetSpecificInformationByType([FromBody] List<GetSpecificInformationRequest> variablesFilter, TypeFileDepositEnum type)
        {
            IHAFileReadOnlyDeposit instanceClass = _fileDepositFactory.CreateClassInstanceByType(type);

            return Ok(await instanceClass.GetSpecificInformation(variablesFilter));
        }

        [HttpPost("GetDatatableSpecificInformationByType")]
        public async Task<IActionResult> GetDatatableSpecificInformationByType([FromBody] GetDatatableSpecificInfosRequest request)
        {
            IHAFileReadOnlyDeposit instanceClass = _fileDepositFactory.CreateClassInstanceByType(request.type);
            var result = await instanceClass.GetSpecificInformation(request.variablesFilter);
            var filteredResult = result.DatatableFilter(request.requestDatatable, out int? count);

            ServerSideResponse<GetInformationsResponse> response = new ServerSideResponse<GetInformationsResponse>();
            response.recordsFiltered = (int)count;
            response.sums = new Dictionary<string, object>();
            response.recordsTotal = filteredResult.Count;
            response.data = filteredResult;
            response.draw = request.requestDatatable.draw;

            return Ok(response);
        }

        [HttpGet("GetDocumentViewerByTag/{tableId}/{tag}")]
        public async Task<IActionResult> GetDocumentViewerByTag(int tableId, string tag) {
            var instanceClass = _fileDepositFactory.CreateClassInstanceByTag(tag);
            if (instanceClass == null)
            {
                return Ok(new { error = "tag not find" });
            }
            return Ok(await instanceClass.GetDocumentViewer(tableId));
        }

        [HttpGet("GetDocumentViewerByType/{tableId}/{type}")]
        public async Task<IActionResult> GetDocumentViewerByType(int tableId, TypeFileDepositEnum type)
        {
            IHAFileReadOnlyDeposit instanceClass = _fileDepositFactory.CreateClassInstanceByType(type);

            return Ok(await instanceClass.GetDocumentViewer(tableId));
        }

        //CRUD File deposit
        [HttpPost("GetAllFileDeposit")]
        public async Task<IActionResult> GetAllFileDeposit([FromBody] ServerSideRequest request)
        {
            return Ok(await _fileDepositService.GetAllFileDeposit(request));
        }

        [HttpGet("DeleteFileDeposit/{fileDepositId}")]
        public async Task<IActionResult> DeleteFileDeposit(int fileDepositId)
        {
            return Ok(await _fileDepositService.DeleteFileDeposit(fileDepositId));
        }

        [HttpPost("UpdateFileDeposit")]
        public async Task<IActionResult> UpdateFileDeposit([FromBody] FileDepositRequest FileDepositToUpdate)
        {
            return Ok(await _fileDepositService.UpdateFileDeposit(FileDepositToUpdate));
        }

        [HttpPost("AddFileDeposit")]
        public async Task<IActionResult> AddFileDeposit([FromBody] FileDepositRequest FileDepositToAdd)
        {
            return Ok(await _fileDepositService.AddFileDeposit(FileDepositToAdd));
        }

        [HttpGet("GetAllFileDepositActive")]
        public async Task<IActionResult> GetAllFileDepositActive()
        {
            return Ok(await _fileDepositService.GetAllFileDepositActive());
        }
    }
}
