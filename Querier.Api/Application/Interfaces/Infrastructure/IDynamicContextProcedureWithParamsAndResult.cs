using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    public interface IDynamicContextProcedureWithParamsAndResult
    {
        Task<List<dynamic>> ExecuteAsync(Dictionary<string, object> parameters);
    }
}