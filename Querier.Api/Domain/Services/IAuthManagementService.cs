using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Domain.Services;

public interface IAuthManagementService
{
    public Task<SignUpResponse> SignUp(SignUpRequest user);
    public Task<SignUpResponse> SignIn(SignInRequest user);
    public Task<AuthResult> RefreshToken(TokenRequest tokenRequest);
}