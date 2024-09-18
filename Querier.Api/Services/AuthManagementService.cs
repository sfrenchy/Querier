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

namespace Querier.Api.Services
{
    public interface IUserManagerService : IDisposable
    {
        Task<ApiUser> FindByEmailAsync(string email);
        Task<bool> CheckPasswordAsync(ApiUser user, string password);
        Task<IList<string>> GetRolesAsync(ApiUser user);
        Task<IdentityResult> CreateAsync(ApiUser user, string password);
        Task<IdentityResult> AddToRoleAsync(ApiUser user, string role);
        UserManager<ApiUser> Instance { get; }
    }

    public class UserManagerService : IUserManagerService
    {
        private readonly UserManager<ApiUser> _userManager;

        public UserManager<ApiUser> Instance { get { return _userManager; } }
        public UserManagerService(UserManager<ApiUser> userManager)
        {
            _userManager = userManager;
        }
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
        public Task<RegistrationResponse> Register(UserRegistrationRequest user);
        public Task<RegistrationResponse> Login(UserLoginRequest user);
        public Task<AuthResult> RefreshToken(TokenRequest tokenRequest);
        public Task<RegistrationResponse> GoogleLogin(GoogleLoginRequest user);
    }

    public class AuthManagementService : IAuthManagementService
    {
        private readonly IUserManagerService _userManager;
        private readonly JwtConfig _jwtConfig;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IThemeService _themeService;

        public AuthManagementService(IUserManagerService userManager, IOptionsMonitor<JwtConfig> optionsMonitor, TokenValidationParameters tokenValidationParameters, IDbContextFactory<ApiDbContext> contextFactory, IThemeService themeService)
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
            _tokenValidationParameters = tokenValidationParameters;
            _contextFactory = contextFactory;
            _themeService = themeService;
        }
        public async Task<RegistrationResponse> Register(UserRegistrationRequest user)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var result = await UserMethods.Register(user, _userManager.Instance, _jwtConfig, apidbContext);
                return result;
            }
        }

        public async Task<RegistrationResponse> Login(UserLoginRequest user)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);
                if (existingUser == null)
                {
                    // We dont want to give to much information on why the request has failed for security reasons
                    return new RegistrationResponse()
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
                    _themeService.CreateDefaultTheme(existingUser.Id);
                    AuthResult r = await UserMethods.GenerateJwtToken(existingUser, _jwtConfig, apidbContext);
                    var roles = await _userManager.GetRolesAsync(existingUser);

                    return new RegistrationResponse()
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
                        DateFormat = existingUser.DateFormat,
                        Img = existingUser.Img,
                        LanguageCode = existingUser.LanguageCode
                    };
                }
                else
                {
                    // We dont want to give to much information on why the request has failed for security reasons
                    return new RegistrationResponse()
                    {
                        Success = false,
                        Errors = new List<string>(){
                            "Invalid authentication request"
                        }
                    };
                }
            }
        }

        public async Task<RegistrationResponse> GoogleLogin(GoogleLoginRequest user)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var plugin = ServiceActivator.GetScope().ServiceProvider.GetService(typeof(IQPlugin)) as IQPlugin;
                //Check google token, it will crash if it isn't valid 
                var isTokenValid = await UserMethods.VerifyGoogleToken(user.AuthToken);
                var existingUser = await _userManager.FindByEmailAsync(user.Email);

                //if user doesn't exist, create it in DB
                if (existingUser == null)
                {
                    //generate a password
                    var guid = Guid.NewGuid().ToString();
                    var pw = ExtensionMethods.GetSHA1Hash(guid);

                    var gUser = new ApiUser
                    {
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        UserName = user.LastName,
                    };

                    var isCreated = await _userManager.CreateAsync(gUser, pw);

                    if (isCreated.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(gUser, "User");
                        var result = await UserMethods.GenerateJwtToken(gUser, _jwtConfig, apidbContext);
                        _themeService.CreateDefaultTheme(gUser.Id);

                        var userCreated = await _userManager.FindByEmailAsync(gUser.Email);

                        plugin.QuerierUserCreated(userCreated);

                        return new RegistrationResponse()
                        {
                            Success = result.Success,
                            Email = user.Email,
                            LastName = gUser.LastName,
                            FirstName = gUser.FirstName,
                            Id = userCreated.Id,
                            Token = result.Token,
                            RefreshToken = result.RefreshToken,
                            LanguageCode = userCreated.LanguageCode
                        };
                    }
                    else
                    {
                        return new RegistrationResponse()
                        {
                            Success = false,
                            Errors = isCreated.Errors.Select(x => x.Description).ToList()
                        };
                    }
                }
                else
                {
                    AuthResult r = await UserMethods.GenerateJwtToken(existingUser, _jwtConfig, apidbContext);
                    _themeService.CreateDefaultTheme(existingUser.Id);
                    return new RegistrationResponse()
                    {
                        Success = r.Success,
                        Email = user.Email,
                        LastName = existingUser.LastName,
                        FirstName = existingUser.FirstName,
                        Id = existingUser.Id,
                        Token = r.Token,
                        RefreshToken = r.RefreshToken,
                        DateFormat = existingUser.DateFormat,
                        Img = existingUser.Img,
                        LanguageCode = existingUser.LanguageCode
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
