using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Requests.DBConnection;
using Querier.Api.Application.DTOs.Responses.DBConnection;
using Querier.Api.Domain.Entities.QDBConnection;

namespace Querier.Api.Domain.Services
{

    public interface IDBConnectionService
    {
        Task<AddDBConnectionResponse> AddConnectionAsync(AddDBConnectionRequest request);
        Task<DeleteDBConnectionResponse> DeleteDBConnectionAsync(DeleteDBConnectionRequest request);
        Task<List<QDBConnectionResponse>> GetAll();
    }
}