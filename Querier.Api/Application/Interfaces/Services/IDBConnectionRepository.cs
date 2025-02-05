using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IDbConnectionRepository
    {
        Task<int> AddDbConnectionAsync(DBConnection dbConnection);
        Task DeleteDbConnectionAsync(int dbConnectionId);
        Task<List<DBConnectionDto>> GetAllDbConnectionsAsync();
        
        Task<DBConnection> FindByIdAsync(int dbConnectionId);
        Task<DBConnection?> FindByIdWithControllersAsync(int dbConnectionId);
        Task<List<EndpointDescription>> FindEndpointsAsync(int dbConnectionId, string? controller = null, string? targetTable = null, string? action = null);
        public Task<byte[]> GetDLLStreamAsync(int connectionId);
        public Task<byte[]> GetPDBStreamAsync(int connectionId);
    }
}