using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Querier.Api.Services.Repositories.User;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Application.Interfaces.Services.User;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.DependencyInjection;

namespace Querier.Api.Domain.Services
{

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

    public class AuthManagementService : IAuthManagementService
    {
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly IUserManagerService _userManager;
        private readonly ISettingService _settingService;
        private readonly ApiDbContext _context;

        public AuthManagementService(
            IUserManagerService userManager,
            ISettingService settingService,
            ApiDbContext context,
            TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _settingService = settingService;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
        }

        public async Task<SignUpResponse> SignUp(SignUpRequest user)
        {
            var result = await UserMethods.Register(user, _userManager.Instance, _context, _settingService);
            return result;
        }

        public async Task<SignUpResponse> SignIn(SignInRequest user)
        {
            var existingUser = await _userManager.FindByEmailAsync(user.Email);
            if (existingUser == null)
            {
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
                AuthResult r = await UserMethods.GenerateJwtToken(existingUser, _context, _settingService);
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
                return new SignUpResponse()
                {
                    Success = false,
                    Errors = new List<string>(){
                        "Invalid authentication request"
                    }
                };
            }
        }

        public async Task<AuthResult> RefreshToken(TokenRequest tokenRequest)
        {
            var res = await UserMethods.VerifyToken(tokenRequest, _tokenValidationParameters, _userManager.Instance, _context, _settingService);
            return res;
        }
    }
}
