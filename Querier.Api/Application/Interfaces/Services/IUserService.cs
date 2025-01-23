using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IUserService
    {
        public Task<ApiUserDto> GetByIdAsync(string id);
        public Task<ApiUserDto> GetByEmailAsync(string email);
        public Task<bool> SetUserRolesAsync(string id, List<RoleDto> roles);
        public Task<bool> AddAsync(ApiUserCreateDto user);
        public Task<bool> UpdateAsync(ApiUserUpdateDto user);
        public Task<bool> DeleteByIdAsync(string id);
        public Task<string> GetPasswordHashAsync(string idUser);
        public Task<IEnumerable<ApiUserDto>> GetAllAsync();
        public Task<object> ResetPasswordAsync(ResetPasswordDto resetPasswordInfos);
        public Task<bool> EmailConfirmationAsync(EmailConfirmationDto emailConfirmation);
        public Task<(bool Succeeded, string Error)> ConfirmEmailAndSetPasswordAsync(EmailConfirmationSetPasswordDto request);
        public Task<bool> SendConfirmationEmailAsync(ApiUser user, string token);
        public Task<ApiUserDto> GetCurrentUserAsync(ClaimsPrincipal userClaims);
        public Task<bool> ResendConfirmationEmailAsync(string userId);
    }
}
