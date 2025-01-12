using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IAuthenticationService
    {
        Task<SignUpResultDto> SignUp(SignUpDto user);
        Task<SignUpResultDto> SignIn(SignInDto user);
        Task<AuthResultDto> RefreshToken(RefreshTokenDto tokenRequest);
    }
}