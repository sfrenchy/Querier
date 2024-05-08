using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Models.Datatable;

namespace Querier.Api.Models.Interfaces
{
    public interface IDynamicContextProcedureWithParamsAndResult
    {
        
        Task<List<dynamic>?> DatasAsync(Dictionary<string, object> parameters);
        Task<ServerSideResponse<dynamic>?> ReportDatasAsync(Dictionary<string, object> parameters, ServerSideRequest DatatableParams);
    }
}