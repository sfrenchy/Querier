using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.DBConnection;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IDbConnectionRepository
    {
        Task<int> AddDbConnectionAsync(DBConnection dbConnection);
        Task DeleteDbConnectionAsync(int dbConnectionId);
        Task<List<DBConnection>> GetAllDbConnectionsAsync();
        
        Task<DBConnection> FindByIdAsync(int dbConnectionId);
    }
}