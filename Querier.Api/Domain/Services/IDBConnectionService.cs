using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Requests.DBConnection;
using Querier.Api.Application.DTOs.Responses.DBConnection;
using Querier.Api.Domain.Entities.QDBConnection;
using static Querier.Api.Domain.Services.DBConnectionService;

namespace Querier.Api.Domain.Services
{
    public interface IDBConnectionService
    {
        Task<AddDBConnectionResponse> AddConnectionAsync(AddDBConnectionRequest connection);
        Task<DeleteDBConnectionResponse> DeleteDBConnectionAsync(DeleteDBConnectionRequest request);
        Task<List<QDBConnectionResponse>> GetAll();
        Task<DatabaseSchemaResponse> GetDatabaseSchema(int connectionId);
        Task<QueryAnalysisResponse> GetQueryObjects(int connectionId, string query);
        Task<List<DatabaseServerInfo>> EnumerateServersAsync(string databaseType);
        Task<SourceDownload> GetConnectionSourcesAsync(int connectionId);
    }
}