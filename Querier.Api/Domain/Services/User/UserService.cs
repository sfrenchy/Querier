using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Querier.Api.Services.Repositories.User;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services.User;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Application.Interfaces.Services.Role;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Domain.Services.User
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _configuration;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IEmailSendingService _emailSending;
        private readonly ILogger<UserRepository> _logger;
        private readonly IUserRepository _repo;
        private readonly IRoleService _roleService;

        private readonly UserManager<ApiUser> _userManager;
        private readonly ISettingService _settings;

        // private readonly IQPlugin _herdiaApp;
        public UserService(IDbContextFactory<ApiDbContext> contextFactory, ISettingService settings, IUserRepository repo, ILogger<UserRepository> logger, UserManager<ApiUser> userManager, IEmailSendingService emailSending, IConfiguration configuration, IRoleService roleService)
        {
            _repo = repo;
            _logger = logger;
            _userManager = userManager;
            _emailSending = emailSending;
            _configuration = configuration;
            _contextFactory = contextFactory;
            _roleService = roleService;
            _settings = settings;
        }

        public async Task<bool> Add(UserCreateDto user)
        {
            var foundUser = await _repo.GetByEmail(user.Email);
            if (foundUser != null)
            {
                _logger.LogError($"User with email {user.Email} already exists");
                return false;
            }
            var newUser = MapToModel(user);
            if (!await _repo.Add(newUser))
            {
                return false;
            }

            var roles = await _roleService.GetAll();
            var selectedRoles = roles.Where(r => user.Roles.Contains(r.Name))
                .Select(r => new ApiRole { Id = r.Id, Name = r.Name })
                .ToArray();
            return await _repo.AddRole(newUser, selectedRoles);
        }

        public async Task<bool> Edit(UserUpdateDto user)
        {
            var foundUser = await _repo.GetById(user.Id);
            if (foundUser == null)
            {
                return false;
            }

            MapToModel(user, foundUser);
            if (!await _repo.Edit(foundUser))
            {
                return false;
            }

            var roles = await _roleService.GetAll();
            var selectedRoles = roles.Where(r => user.Roles.Contains(r.Name))
                .Select(r => new ApiRole { Id = r.Id, Name = r.Name })
                .ToArray();

            await _repo.RemoveRoles(foundUser);

            return await _repo.AddRole(foundUser, selectedRoles);
        }

        public async Task<bool> Update(UserUpdateDto user)
        {
            return await Edit(user);
        }

        public async Task<bool> Delete(string id)
        {
            return await _repo.Delete(id);
        }

        public async Task<UserDto> View(string id)
        {
            var userAndRoles = await _repo.GetWithRoles(id);
            if (userAndRoles == null)
            {
                _logger.LogError($"User with id {id} not found");
                return null;
            }
            var vm = MapToVM(userAndRoles.Value.user);
            vm.Roles = await _roleService.GetRolesForUser(id);
            return vm;
        }

        public async Task<List<UserDto>> GetAll()
        {
            List<UserDto> result = new List<UserDto>();
            var userList = await _userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            userList.ForEach(user =>
            {
                var vm = MapToVM(user);
                vm.Roles = user.UserRoles?.Select(ur => new RoleDto { Name = ur.Role.Name }).ToList() ?? new List<RoleDto>();
                result.Add(vm);
            });
            return result;
        }

        public async Task<string> GetPasswordHash(string idUser)
        {
            ApiUser searchUser = await _repo.GetById(idUser);
            if (searchUser == null)
                return string.Empty;

            return searchUser.PasswordHash;
        }

        public async Task<object> ResetPassword(ResetPasswordDto reset_password_infos)
        {
            var user = await _userManager.FindByEmailAsync(reset_password_infos.Email);
            object response;

            //check if the user exist or not
            if (user == null)
            {
                response = new { success = false, message = "User not find, try again" };
                return response;
            }

            //reset password
            var resetPassResult = await _userManager.ResetPasswordAsync(user, reset_password_infos.Token, reset_password_infos.Password);

            if (resetPassResult.Succeeded)
            {
                response = new { success = true, message = "Password has been changed" };
                return response;
            }
            else
            {

                var errorsArray = resetPassResult.Errors.ToArray();
                string[] ArrayErrorsStringResult = new string[errorsArray.Length];
                for (int i = 0; i < errorsArray.Length; i++)
                {
                    ArrayErrorsStringResult[i] = errorsArray[i].Code;
                }


                response = new { success = false, errors = ArrayErrorsStringResult };
                return response;
            }
        }

        public async Task<bool> EmailConfirmation(EmailConfirmationDto emailConfirmation)
        {
            string token = Uri.UnescapeDataString(emailConfirmation.Token);
            var user = await _userManager.FindByEmailAsync(emailConfirmation.Email);
            if (user == null)
                return false;
            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public async Task<(bool Succeeded, string Error)> ConfirmEmailAndSetPassword(EmailConfirmationSetPasswordDto request)
        {
            try
            {
                if (request.Password != request.ConfirmPassword)
                {
                    return (false, "Les mots de passe ne correspondent pas.");
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return (false, "Utilisateur non trouvé.");
                }

                if (user.EmailConfirmed)
                {
                    return (false, "Cet email est déjà confirmé.");
                }

                var decodedToken = Uri.UnescapeDataString(request.Token)
                    .Replace(" ", "+");

                var confirmResult = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (!confirmResult.Succeeded)
                {
                    _logger.LogError($"Email confirmation failed for user {user.Id}. Errors: {string.Join(", ", confirmResult.Errors.Select(e => e.Description))}");
                    return (false, "Le lien de confirmation n'est plus valide.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);

                if (!passwordResult.Succeeded)
                {
                    _logger.LogError($"Password reset failed for user {user.Id}. Errors: {string.Join(", ", passwordResult.Errors.Select(e => e.Description))}");
                    return (false, "Le mot de passe ne respecte pas les critères de sécurité.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la confirmation d'email");
                return (false, "Une erreur est survenue.");
            }
        }

        public async Task<bool> SendConfirmationEmail(ApiUser user, string token)
        {
            try
            {
                string tokenValidity = await _settings.GetSettingValue("api:email:confirmationTokenValidityLifeSpanDays", "2");
                string baseUrl = string.Concat(
                    await _settings.GetSettingValue("api:scheme", "https"), "://",
                    await _settings.GetSettingValue("api:host", "localhost"), ":",
                    await _settings.GetSettingValue("api:port", "5001")
                );

                var encodedToken = Uri.EscapeDataString(token);

                var parameters = new Dictionary<string, string>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "Token", encodedToken },
                    { "Email", user.Email },
                    { "TokenValidity", tokenValidity },
                    { "BaseUrl", baseUrl }
                };

                return await _emailSending.SendTemplatedEmailAsync(
                    user.Email,
                    "Confirmation d'email",
                    "EmailConfirmation",
                    "fr",
                    parameters
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending confirmation email");
                return false;
            }
        }

        public async Task<UserDto> GetCurrentUser(ClaimsPrincipal userClaims)
        {
            var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                var userEmail = userClaims.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    _logger.LogWarning("No user identifier found in token");
                    return null;
                }

                var userByEmail = await _userManager.FindByEmailAsync(userEmail);
                if (userByEmail == null)
                {
                    _logger.LogWarning($"No user found with email: {userEmail}");
                    return null;
                }
                userId = userByEmail.Id;
            }

            return await View(userId);
        }

        public async Task<bool> ResendConfirmationEmail(string userEmail)
        {
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                _logger.LogWarning($"User not found with email: {userEmail}");
                return false;
            }

            if (user.EmailConfirmed)
            {
                _logger.LogWarning($"Email already confirmed for user: {userEmail}");
                return false;
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            return await SendConfirmationEmail(user, token);
        }

        private void MapToModel(UserUpdateDto user, ApiUser updateUser)
        {
            updateUser.FirstName = user.FirstName;
            updateUser.LastName = user.LastName;
            updateUser.Email = user.Email;
        }

        private ApiUser MapToModel(UserCreateDto user)
        {
            return new ApiUser
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName
            };
        }

        private UserDto MapToVM(ApiUser user)
        {
            return new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                IsEmailConfirmed = user.EmailConfirmed,
                UserName = user.UserName,
                Roles = user.UserRoles?.Select(ur => new RoleDto { Name = ur.Role.Name }).ToList() ?? new List<RoleDto>()
            };
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync()
        {
            // Charger les utilisateurs avec leurs rôles
            var users = await _userManager.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var userResponses = new List<UserDto>();

            foreach (var user in users)
            {
                // Récupérer explicitement les rôles pour chaque utilisateur
                var roles = await _roleService.GetRolesForUser(user.Id);
                var userResponse = MapToVM(user);
                userResponse.Roles = roles; // Utiliser les rôles récupérés via le roleService
                userResponses.Add(userResponse);
            }

            return userResponses;
        }
    }
}
