using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IUserManagerService
    {
        UserManager<ApiUser> Instance { get; }
        Task<ApiUser> FindByEmailAsync(string email);
        Task<bool> CheckPasswordAsync(ApiUser user, string password);
        Task<IList<string>> GetRolesAsync(ApiUser user);
        Task<IdentityResult> CreateAsync(ApiUser user, string password);
        Task<IdentityResult> AddToRoleAsync(ApiUser user, string role);
    }
}
