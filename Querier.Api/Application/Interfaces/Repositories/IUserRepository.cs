using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<ApiUser> GetByIdAsync(string id);
        Task<(ApiUser user, List<ApiRole> roles)?> GetWithRolesAsync(string id);
        Task<ApiUser> GetByEmailAsync(string email);
        Task<IdentityResult> AddAsync(ApiUser user);
        Task<bool> UpdateAsync(ApiUser user);
        Task<bool> DeleteAsync(string id);
        Task<List<ApiUser>> GetAllAsync();
        Task<bool> AddRoleAsync(ApiUser user, ApiRole[] role);
        Task<bool> RemoveRolesAsync(ApiUser user);
        Task<IdentityResult> ResetPasswordAsync(ApiUser user, string token, string password);
        Task<IdentityResult> ConfirmEmailAsync(ApiUser user, string token);
        Task<string> GeneratePasswordResetTokenAsync(ApiUser user);
        Task<string> GenerateEmailConfirmationTokenAsync(ApiUser user);
        Task<List<ApiRole>> GetRolesAsync(ApiUser user);
        Task<bool> CheckPasswordAsync(ApiUser user, string userPassword);
        Task<bool> IsEmailConfirmedAsync(ApiUser user);
    }
}