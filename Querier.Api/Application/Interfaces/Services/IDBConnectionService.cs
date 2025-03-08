using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.DTOs;
using static Querier.Api.Domain.Services.DbConnectionService;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IDbConnectionService
    {
        Task<DBConnectionCreateResultDto> AddConnectionAsync(DBConnectionCreateDto connection);
        Task DeleteDbConnectionAsync(int dbConnectionId);
        Task<List<DBConnectionDto>> GetAllAsync();
        Task<DBConnectionDatabaseSchemaDto> GetDatabaseSchemaAsync(int connectionId);
        Task<DBConnectionQueryAnalysisDto> GetQueryObjectsAsync(int connectionId, string query);
        Task<List<DBConnectionDatabaseServerInfoDto>> EnumerateServersAsync(string databaseType);
        Task<SourceDownload> GetConnectionSourcesAsync(int connectionId);
        Task<List<DBConnectionEndpointInfoDto>> GetEndpointsAsync(int connectionId, string? targetTable, string? controller, string? action);
        Task<List<DBConnectionControllerInfoDto>> GetControllersAsync(int connectionId);
        Task<DBConnectionDto> GetByIdAsync(int id);
        Task<IDbContextFactory<DbContext>> GetDbContextFactoryByContextTypeFullNameAsync(string contextTypeFullName);
        Task<IDbContextFactory<DbContext>> GetDbContextFactoryByIdAsync(int id);
        Task<IDbContextFactory<DbContext>> GetReadOnlyDbContextFactoryByIdAsync(int id);
    }
}