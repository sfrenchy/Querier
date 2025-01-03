using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Requests.DBConnection;
using Querier.Api.Application.DTOs.Responses.DBConnection;
using Querier.Api.Domain.Entities.QDBConnection;

public interface IDBConnectionService
{
    Task<AddDBConnectionResponse> AddConnectionAsync(AddDBConnectionRequest request);
    Task<DeleteDBConnectionResponse> DeleteDBConnectionAsync(DeleteDBConnectionRequest request);
    Task<List<QDBConnectionResponse>> GetAll();
    Task<DatabaseSchemaResponse> GetDatabaseSchema(int connectionId);
    Task<QueryAnalysisResponse> GetQueryObjects(int connectionId, string query);
    
    /// <summary>
    /// Énumère les serveurs de base de données disponibles sur le réseau
    /// </summary>
    /// <param name="databaseType">Type de base de données (SQLServer, MySQL, PostgreSQL)</param>
    /// <returns>Liste des serveurs trouvés avec leurs informations</returns>
    Task<List<DatabaseServerInfo>> EnumerateServersAsync(string databaseType);
}