using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services.User
{
    public interface IUserService
    {
        public Task<UserDto> View(string id);
        public Task<bool> Add(UserCreateDto user);
        public Task<bool> Update(UserUpdateDto user);
        public Task<bool> Delete(string id);
        public Task<string> GetPasswordHash(string idUser);
        public Task<List<UserDto>> GetAll();
        public Task<object> ResetPassword(ResetPasswordDto reset_password_infos);
        public Task<bool> EmailConfirmation(EmailConfirmationDto emailConfirmation);
        public Task<(bool Succeeded, string Error)> ConfirmEmailAndSetPassword(EmailConfirmationSetPasswordDto request);
        public Task<bool> SendConfirmationEmail(ApiUser user, string token);
        public Task<UserDto> GetCurrentUser(ClaimsPrincipal userClaims);
        public Task<bool> ResendConfirmationEmail(string userId);
        public Task<IEnumerable<UserDto>> GetAllAsync();
    }
}
