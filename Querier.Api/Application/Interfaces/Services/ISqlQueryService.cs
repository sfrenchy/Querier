using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Common.Models;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface ISqlQueryService
    {
        Task<IEnumerable<SqlQueryDto>> GetAllQueriesAsync(string userId);
        Task<SqlQueryDto> GetQueryByIdAsync(int id);
        Task<SqlQueryDto> CreateQueryAsync(SqlQueryDto query, Dictionary<string, object> sampleParameters = null);
        Task<SqlQueryDto> UpdateQueryAsync(SqlQueryDto query, Dictionary<string, object> sampleParameters = null);
        Task DeleteQueryAsync(int id);

        Task<PagedResult<dynamic>> ExecuteQueryAsync(int queryId, DataRequestParametersWtihSQLParametersDto dataRequestParameters);
    }
}