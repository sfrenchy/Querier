using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Common.Models;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface ISqlQueryService
    {
        Task<IEnumerable<SQLQueryDTO>> GetAllQueriesAsync(string userId);
        Task<SQLQueryDTO> GetQueryByIdAsync(int id);
        Task<SQLQueryDTO> CreateQueryAsync(SQLQueryDTO query, Dictionary<string, object> sampleParameters = null);
        Task<SQLQueryDTO> UpdateQueryAsync(SQLQueryDTO query, Dictionary<string, object> sampleParameters = null);
        Task DeleteQueryAsync(int id);

        Task<PagedResult<dynamic>> ExecuteQueryAsync(int queryId, Dictionary<string, object> parameters,
            int pageNumber = 1, int pageSize = 0);
    }
}