using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IDbConnectionRepository
    {
        Task<int> AddDbConnectionAsync(DBConnection dbConnection);
        Task DeleteDbConnectionAsync(int dbConnectionId);
        Task<List<DBConnection>> GetAllDbConnectionsAsync();
        
        Task<DBConnection> FindByIdAsync(int dbConnectionId);
        Task<DBConnection?> FindByIdWithControllersAsync(int dbConnectionId);
        Task<List<EndpointDescription>> FindEndpointsAsync(int dbConnectionId, string? controller = null, string? targetTable = null, string? action = null);
    }
}