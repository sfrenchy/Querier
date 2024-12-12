using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Models;
using Querier.Api.Models.QDBConnection;
using Querier.Api.Models.Requests;
using Querier.Api.Models.Responses;

namespace Querier.Api.Services
{

    public interface IDBConnectionService
    {
        Task<AddDBConnectionResponse> AddConnectionAsync(AddDBConnectionRequest request);
        Task<DeleteDBConnectionResponse> DeleteDBConnectionAsync(DeleteDBConnectionRequest request);
        Task<List<QDBConnectionResponse>> GetAll();
    }
}