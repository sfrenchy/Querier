using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Controllers;
using Querier.Api.Domain.Common.Models;

namespace Querier.Api.Application.Interfaces.Services;

public interface ILinqQueryService
{
    Task<IEnumerable<LinqQueryDto>> GetAllQueriesAsync(string userId);
    Task<LinqQueryDto> GetQueryByIdAsync(int id);
    Task<LinqQueryDto> CreateQueryAsync(LinqQueryDto query, Dictionary<string, object> sampleParameters = null);
    Task<LinqQueryDto> UpdateQueryAsync(LinqQueryDto query, Dictionary<string, object> sampleParameters = null);
    Task DeleteQueryAsync(int id);

    Task<DataPagedResult<dynamic>> ExecuteQueryAsync(int queryId, DataRequestParametersWithParametersDto dataRequestParameters);
}