using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Querier.Api.Services.Repositories.User;
using Querier.Api.Tools;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;

namespace Querier.Api.Services
{
    public interface IUserManagerService : IDisposable
    {
        UserManager<ApiUser> Instance { get; }
        Task<ApiUser> FindByEmailAsync(string email);
        Task<bool> CheckPasswordAsync(ApiUser user, string password);
        Task<IList<string>> GetRolesAsync(ApiUser user);
        Task<IdentityResult> CreateAsync(ApiUser user, string password);
        Task<IdentityResult> AddToRoleAsync(ApiUser user, string role);
    }

    public class UserManagerService : IUserManagerService
    {
        private readonly UserManager<ApiUser> _userManager;

        public UserManagerService(UserManager<ApiUser> userManager)
        {
            _userManager = userManager;
        }

        public UserManager<ApiUser> Instance { get { return _userManager; } }

        public Task<IdentityResult> AddToRoleAsync(ApiUser user, string role)
        {
            return _userManager.AddToRoleAsync(user, role);
        }

        public Task<bool> CheckPasswordAsync(ApiUser user, string password)
        {
            using (var serviceScope = ServiceActivator.GetScope())
            {
                UserManager<ApiUser> userManager = serviceScope.ServiceProvider.GetService<UserManager<ApiUser>>();

                return userManager.CheckPasswordAsync(user, password);
            }
        }

        public Task<IdentityResult> CreateAsync(ApiUser user, string password)
        {
            return _userManager.CreateAsync(user, password);
        }

        public void Dispose()
        {
            _userManager.Dispose();
            Console.WriteLine("- IUserManagerService was disposed!");
        }

        public Task<ApiUser> FindByEmailAsync(string email)
        {
            return _userManager.FindByEmailAsync(email);
        }

        public Task<IList<string>> GetRolesAsync(ApiUser user)
        {
            return _userManager.GetRolesAsync(user);
        }
    }

    public interface IAuthManagementService
    {
        public Task<SignUpResponse> SignUp(SignUpRequest user);
        public Task<SignUpResponse> SignIn(SignInRequest user);
        public Task<AuthResult> RefreshToken(TokenRequest tokenRequest);
    }

    public class AuthManagementService : IAuthManagementService
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly JwtConfig _jwtConfig;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly IUserManagerService _userManager;

        public AuthManagementService(IUserManagerService userManager, IOptionsMonitor<JwtConfig> optionsMonitor, TokenValidationParameters tokenValidationParameters, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
            _tokenValidationParameters = tokenValidationParameters;
            _contextFactory = contextFactory;
        }

        public async Task<SignUpResponse> SignUp(SignUpRequest user)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var result = await UserMethods.Register(user, _userManager.Instance, _jwtConfig, apidbContext);
                return result;
            }
        }

        public async Task<SignUpResponse> SignIn(SignInRequest user)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);
                if (existingUser == null)
                {
                    // We dont want to give to much information on why the request has failed for security reasons
                    return new SignUpResponse()
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Invalid authentication request"
                        }

                    };
                }

                var PasswordisCorrect = await _userManager.Instance.CheckPasswordAsync(existingUser, user.Password);
                var EmailConfirmed = await _userManager.Instance.IsEmailConfirmedAsync(existingUser);

                if (PasswordisCorrect && EmailConfirmed)
                {
                    AuthResult r = await UserMethods.GenerateJwtToken(existingUser, _jwtConfig, apidbContext);
                    var roles = await _userManager.GetRolesAsync(existingUser);

                    return new SignUpResponse()
                    {
                        Id = existingUser.Id,
                        FirstName = existingUser.FirstName,
                        LastName = existingUser.LastName,
                        Roles = roles.ToList(),
                        RefreshToken = r.RefreshToken,
                        Success = r.Success,
                        Token = r.Token,
                        Email = existingUser.Email,
                        UserName = existingUser.UserName,
                    };
                }
                else
                {
                    // We dont want to give to much information on why the request has failed for security reasons
                    return new SignUpResponse()
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Invalid authentication request"
                        }
                    };
                }
            }
        }

        

        public async Task<AuthResult> RefreshToken(TokenRequest tokenRequest)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var res = await UserMethods.VerifyToken(tokenRequest, _tokenValidationParameters, _userManager.Instance, _jwtConfig, apidbContext);
                return res;
            }
        }
    }
}
