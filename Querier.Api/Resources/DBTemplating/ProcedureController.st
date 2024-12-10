using $nameSpace$.ProcedureParameters;
using $nameSpace$.ProcedureReportRequests;
using $nameSpace$.ProcedureResultSets;
using $nameSpace$.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Querier.Api.Models.Datatable;

namespace $nameSpace$.Mvc
{
    $procedureList: {procedure|
    [Authorize]
    [ApiController]
    [Route("api/v1/$contextRoute$/[controller]")]
    public partial class $procedure.CSName$Controller : ControllerBase
    {
        private readonly ILogger<$procedure.CSName$Controller> _logger;
        private readonly I$procedure.CSName$Service _$procedure.CSName$Service;
        public $procedure.CSName$Controller(I$procedure.CSName$Service $procedure.CSName$Service,  ILogger<$procedure.CSName$Controller> logger)
        {
            _logger = logger;
            _$procedure.CSName$Service = $procedure.CSName$Service;
        \}

        [HttpPost("Datas")]
        $if (procedure.HasOutput)$
        [ProducesResponseType(typeof(List<$procedure.CSName$Result>), 200)]
        $endif$
        public async Task<IActionResult> DatasAsync($if (procedure.HasParameters)$[FromBody] $procedure.CSName$Params parameters$endif$)
        {
            _logger.LogInformation("$procedure.CSName$DatasAsync action called");
            $if (procedure.HasParameters)$
            _logger.LogDebug("$procedure.CSName$DatasAsync: ", JsonConvert.SerializeObject(parameters));
            $endif$
            
            $if (procedure.HasOutput)$var _ = $endif$await _$procedure.CSName$Service.DatasAsync($if (procedure.HasParameters)$parameters$endif$);
            return new $if(procedure.HasOutput)$OkObjectResult(_)$else$OkResult()$endif$;
        \}

        $if (procedure.HasOutput)$
        [HttpPost("ReportDatas")]
        [ProducesResponseType(typeof(ServerSideResponse<$procedure.CSName$Result>), 200)]
        public async Task<IActionResult> ReportDatasAsync([FromBody] $procedure.CSName$ReportRequest datatableRequest)
        {
                _logger.LogInformation("$procedure.CSName$ReportDatasAsync action called");
                _logger.LogDebug("$procedure.CSName$ReportDatasAsync: ", JsonConvert.SerializeObject(datatableRequest));
                var _ = await _$procedure.CSName$Service.ReportDatasAsync(datatableRequest);
                return new OkObjectResult(_);
        \}
        $endif$
    \}
    }$
\}