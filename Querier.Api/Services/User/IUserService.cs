using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Requests.User;
using Querier.Api.Models.Responses.User;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Querier.Api.Services.User
{
    public interface IUserService
    {
        public Task<UserResponse> View(string id);
        public Task<bool> Add(UserRequest user);
        public Task<bool> Update(UserRequest user);
        public Task<bool> Delete(string id);
        public Task<string> GetPasswordHash(string idUser);
        public Task<List<UserResponse>> GetAll();
        public Task<object> ResetPassword(ResetPassword reset_password_infos);
        public Task<object> CheckPassword(CheckPassword Checkpassword);
        public Task<bool> EmailConfirmation(EmailConfirmation emailConfirmation);
        public Task<(bool Succeeded, string Error)> ConfirmEmailAndSetPassword(EmailConfirmationRequest request);
        public Task<bool> SendConfirmationEmail(ApiUser user, string token);
        public Task<UserResponse> GetCurrentUser(ClaimsPrincipal userClaims);
        public Task<bool> ResendConfirmationEmail(string userId);
        public Task<IEnumerable<UserResponse>> GetAllAsync();
    }
}
