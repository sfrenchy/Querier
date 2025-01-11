using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public interface IRoleRepository
    {
        List<ApiRole> GetAll();
        Task<bool> AddAsync(ApiRole role);
        Task<bool> UpdateAsync(ApiRole role);
        Task<bool> DeleteByIdAsync(string id);
    }
}