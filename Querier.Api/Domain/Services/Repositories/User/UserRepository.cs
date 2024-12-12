using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Querier.Api.Tools;

namespace Querier.Api.Services.Repositories.User
{
    public interface IUserRepository
    {
        Task<ApiUser> GetById(string id);
        Task<(ApiUser user, List<string> roles)?> GetWithRoles(string id);
        Task<ApiUser> GetByEmail(string email);
        Task<bool> Add(ApiUser user);
        Task<bool> Edit(ApiUser user);
        Task<bool> Delete(string id);
        Task<List<ApiUser>> GetAll();
        Task<bool> AddRole(ApiUser user, ApiRole[] role);
        Task<bool> RemoveRoles(ApiUser user);
    }

    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ApiDbContext _context;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IEmailSendingService _emailSending;
        private readonly ILogger<UserRepository> _logger;
        private readonly UserManager<ApiUser> _userManager;
        private readonly ISettingService _settings;
        private readonly IEmailTemplateService _emailTemplateService;
        public UserRepository(ApiDbContext context, UserManager<ApiUser> userManager, IEmailTemplateService emailTemplateService, ISettingService settings, ILogger<UserRepository> logger, IConfiguration configuration, IEmailSendingService emailSending, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _configuration = configuration;
            _emailSending = emailSending;
            _contextFactory = contextFactory;
            _settings = settings;
            _emailTemplateService = emailTemplateService;
        }

        public async Task<(ApiUser user, List<string> roles)?> GetWithRoles(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                
                if (user == null)
                {
                    user = await _userManager.FindByEmailAsync(id);
                }

                if (user == null)
                {
                    _logger.LogError($"User with id/email {id} not found");
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);
                return (user, roles.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user with roles for id/email {id}");
                return null;
            }
        }

        public async Task<ApiUser> GetById(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogError("UserId is null");
                    return null;
                }
                return await _userManager.FindByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, null);
                return null;
            }
        }

        public async Task<ApiUser> GetByEmail(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogError("User email is null");
                    return null;
                }
                return await _userManager.FindByEmailAsync(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, null);
                return null;
            }
        }

        public async Task<bool> Add(ApiUser user)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogError("User cannot be null");
                    return false;
                }

                string generatedPassword = await GenerateRandomPassword();
                var res = await _userManager.CreateAsync(user, generatedPassword);
                if (!res.Succeeded)
                {
                    _logger.LogError($"Erreur lors de l'ajout de l'utilisateur {user.Email}");
                    return false;
                }

                string token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                string tokenValidity = await _settings.GetSettingValue("api:email:confirmationTokenValidityLifeSpanDays", "2");
                string baseUrl = string.Concat(
                    await _settings.GetSettingValue("api:scheme", "https"), "://",
                    await _settings.GetSettingValue("api:host", "localhost"), ":",
                    await _settings.GetSettingValue("api:port", "5001")
                );
                
                await _emailSending.SendTemplatedEmailAsync(
                    user.Email,
                    "Confirmation de votre email",
                    "EmailConfirmation",
                    "en",
                    new Dictionary<string, string> { 
                        { "Token", token }, 
                        { "TokenValidity", tokenValidity }, 
                        { "BaseUrl", baseUrl },
                        { "FirstName", user.FirstName },
                        { "LastName", user.LastName },
                        { "Email", user.Email }
                    }
                );
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, null);
                return false;
            }
        }

        public async Task<bool> Edit(ApiUser user)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogError("User cannot be null");
                    return false;
                }
                var userUpdated = await _userManager.UpdateAsync(user);
                if (!userUpdated.Succeeded)
                {
                    _logger.LogError($"Erreur lors de la modification de l'utilisateur {user.Email}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, null);
                return false;
            }
        }

        public async Task<bool> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogError("UserId is null");
                    return false;
                }
                var foundUser = await _userManager.FindByIdAsync(id);
                if (foundUser != null)
                {
                    var refreshToken = _context.QRefreshTokens.Where(t => t.UserId == foundUser.Id);
                    _context.QRefreshTokens.RemoveRange(refreshToken);

                    var res = await _userManager.DeleteAsync(foundUser);

                    _context.SaveChanges();
                    return res.Succeeded;
                }
                else
                    _logger.LogError($"User with id {id} not found");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, null);
                return false;
            }
        }

        public async Task<List<ApiUser>> GetAll()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<bool> AddRole(ApiUser user, ApiRole[] role)
        {
            try
            {
                var removed = await _userManager.GetRolesAsync(user);
                if (removed.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(user, removed);
                }

                for (var i = 0; i < role.Length; i++)
                {
                    await _userManager.AddToRoleAsync(user, role[i].Name);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, null);
                return false;
            }
        }

        public async Task<bool> RemoveRoles(ApiUser user)
        {
            try
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, userRoles);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing roles for user {user.Id}");
                return false;
            }
        }

        private async Task<string> GenerateRandomPassword()
        {
            var opts = new PasswordOptions()
            {
                RequireDigit = bool.Parse(await _settings.GetSettingValue("api:password:requireDigit", "true")),
                RequireLowercase = bool.Parse(await _settings.GetSettingValue("api:password:requireLowercase", "true")),
                RequireNonAlphanumeric = bool.Parse(await _settings.GetSettingValue("api:password:requireNonAlphanumeric", "true")),
                RequireUppercase = bool.Parse(await _settings.GetSettingValue("api:password:requireUppercase", "true")),
                RequiredLength = int.Parse(await _settings.GetSettingValue("api:password:requiredLength", "12")),
                RequiredUniqueChars = int.Parse(await _settings.GetSettingValue("api:password:requiredUniqueChars", "1"))
            };

            string[] randomChars = new[] {
                "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
                "abcdefghijkmnopqrstuvwxyz",    // lowercase
                "0123456789",                   // digits
                "!@$?_-"                        // non-alphanumeric
            };

            Random rand = new Random(Environment.TickCount);
            List<char> chars = new List<char>();

            if (opts.RequireUppercase)
                chars.Insert(rand.Next(0, chars.Count), 
                    randomChars[0][rand.Next(0, randomChars[0].Length)]);

            if (opts.RequireLowercase)
                chars.Insert(rand.Next(0, chars.Count), 
                    randomChars[1][rand.Next(0, randomChars[1].Length)]);

            if (opts.RequireDigit)
                chars.Insert(rand.Next(0, chars.Count), 
                    randomChars[2][rand.Next(0, randomChars[2].Length)]);

            if (opts.RequireNonAlphanumeric)
                chars.Insert(rand.Next(0, chars.Count), 
                    randomChars[3][rand.Next(0, randomChars[3].Length)]);

            for (int i = chars.Count; i < opts.RequiredLength
                                      || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
            {
                string rcs = randomChars[rand.Next(0, randomChars.Length)];
                chars.Insert(rand.Next(0, chars.Count), 
                    rcs[rand.Next(0, rcs.Length)]);
            }

            return new string(chars.ToArray());
        }
    }
}
