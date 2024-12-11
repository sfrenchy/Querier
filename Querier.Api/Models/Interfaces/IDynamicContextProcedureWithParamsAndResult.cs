using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Models.Datatable;

namespace Querier.Api.Models.Interfaces
{
    public interface IDynamicContextProcedureWithParamsAndResult
    {
        Task<List<dynamic>?> ExecuteAsync(Dictionary<string, object> parameters);
    }
}