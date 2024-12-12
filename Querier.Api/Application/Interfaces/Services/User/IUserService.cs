using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Querier.Api.Application.DTOs.Auth.Email;
using Querier.Api.Application.DTOs.Auth.Password;
using Querier.Api.Application.DTOs.Requests.Auth;
using Querier.Api.Application.DTOs.Requests.User;
using Querier.Api.Application.DTOs.Responses.User;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services.User
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
        public Task<bool> EmailConfirmation(EmailConfirmation emailConfirmation);
        public Task<(bool Succeeded, string Error)> ConfirmEmailAndSetPassword(EmailConfirmationRequest request);
        public Task<bool> SendConfirmationEmail(ApiUser user, string token);
        public Task<UserResponse> GetCurrentUser(ClaimsPrincipal userClaims);
        public Task<bool> ResendConfirmationEmail(string userId);
        public Task<IEnumerable<UserResponse>> GetAllAsync();
    }
}
