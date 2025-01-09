using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using static Querier.Api.Domain.Services.DBConnectionService;

namespace Querier.Api.Domain.Services
{
    public interface IDBConnectionService
    {
        Task<DBConnectionCreateResultDto> AddConnectionAsync(DBConnectionCreateDto connection);
        Task DeleteDBConnectionAsync(int dbConnectionId);
        Task<List<DBConnectionDto>> GetAll();
        Task<DBConnectionDatabaseSchemaDto> GetDatabaseSchema(int connectionId);
        Task<DBConnectionQueryAnalysisDto> GetQueryObjects(int connectionId, string query);
        Task<List<DBConnectionDatabaseServerInfoDto>> EnumerateServersAsync(string databaseType);
        Task<SourceDownload> GetConnectionSourcesAsync(int connectionId);
        Task<List<DBConnectionEndpointInfoDto>> GetEndpointsAsync(int connectionId);
        Task<List<DBConnectionControllerInfoDto>> GetControllersAsync(int connectionId);
    }
}