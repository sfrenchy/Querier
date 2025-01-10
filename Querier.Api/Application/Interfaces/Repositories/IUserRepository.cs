using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<ApiUser> GetById(string id);
        Task<(ApiUser user, List<string> roles)?> GetWithRoles(string id);
        Task<ApiUser> GetByEmail(string email);
        Task<bool> Add(ApiUser user);
        Task<bool> Edit(ApiUser user);
        Task<bool> Delete(string id);
        Task<List<ApiUser>> GetAll();
        Task<bool> AddRole(ApiUser user, ApiRole[] role);
        Task<bool> RemoveRoles(ApiUser user);
    }
}