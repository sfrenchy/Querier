using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Data.Repositories;

namespace Querier.Api.Domain.Services
{
    public class UserService(
        ISettingService settings,
        IUserRepository repo,
        ILogger<UserRepository> logger,
        UserManager<ApiUser> userManager,
        IEmailSendingService emailSending,
        IRoleService roleService)
        : IUserService
    {
        public async Task<bool> Add(UserCreateDto user)
        {
            var foundUser = await repo.GetByEmail(user.Email);
            if (foundUser != null)
            {
                logger.LogError($"User with email {user.Email} already exists");
                return false;
            }
            var newUser = MapToModel(user);
            if (!await repo.Add(newUser))
            {
                return false;
            }

            var roles = await roleService.GetAll();
            var selectedRoles = roles.Where(r => user.Roles.Contains(r.Name))
                .Select(r => new ApiRole { Id = r.Id, Name = r.Name })
                .ToArray();
            return await repo.AddRole(newUser, selectedRoles);
        }

        public async Task<bool> Edit(UserUpdateDto user)
        {
            var foundUser = await repo.GetById(user.Id);
            if (foundUser == null)
            {
                return false;
            }

            MapToModel(user, foundUser);
            if (!await repo.Edit(foundUser))
            {
                return false;
            }

            var roles = await roleService.GetAll();
            var selectedRoles = roles.Where(r => user.Roles.Contains(r.Name))
                .Select(r => new ApiRole { Id = r.Id, Name = r.Name })
                .ToArray();

            await repo.RemoveRoles(foundUser);

            return await repo.AddRole(foundUser, selectedRoles);
        }

        public async Task<bool> Update(UserUpdateDto user)
        {
            return await Edit(user);
        }

        public async Task<bool> Delete(string id)
        {
            return await repo.Delete(id);
        }

        public async Task<UserDto> View(string id)
        {
            var userAndRoles = await repo.GetWithRoles(id);
            if (userAndRoles == null)
            {
                logger.LogError($"User with id {id} not found");
                return null;
            }
            var vm = MapToVM(userAndRoles.Value.user);
            vm.Roles = await roleService.GetRolesForUser(id);
            return vm;
        }

        public async Task<List<UserDto>> GetAll()
        {
            List<UserDto> result = new List<UserDto>();
            var userList = await userManager.Users
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
            ApiUser searchUser = await repo.GetById(idUser);
            if (searchUser == null)
                return string.Empty;

            return searchUser.PasswordHash;
        }

        public async Task<object> ResetPassword(ResetPasswordDto reset_password_infos)
        {
            var user = await userManager.FindByEmailAsync(reset_password_infos.Email);
            object response;

            //check if the user exist or not
            if (user == null)
            {
                response = new { success = false, message = "User not find, try again" };
                return response;
            }

            //reset password
            var resetPassResult = await userManager.ResetPasswordAsync(user, reset_password_infos.Token, reset_password_infos.Password);

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
            var user = await userManager.FindByEmailAsync(emailConfirmation.Email);
            if (user == null)
                return false;
            var result = await userManager.ConfirmEmailAsync(user, token);
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

                var user = await userManager.FindByEmailAsync(request.Email);
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

                var confirmResult = await userManager.ConfirmEmailAsync(user, decodedToken);
                if (!confirmResult.Succeeded)
                {
                    logger.LogError($"Email confirmation failed for user {user.Id}. Errors: {string.Join(", ", confirmResult.Errors.Select(e => e.Description))}");
                    return (false, "Le lien de confirmation n'est plus valide.");
                }

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await userManager.ResetPasswordAsync(user, token, request.Password);

                if (!passwordResult.Succeeded)
                {
                    logger.LogError($"Password reset failed for user {user.Id}. Errors: {string.Join(", ", passwordResult.Errors.Select(e => e.Description))}");
                    return (false, "Le mot de passe ne respecte pas les critères de sécurité.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors de la confirmation d'email");
                return (false, "Une erreur est survenue.");
            }
        }

        public async Task<bool> SendConfirmationEmail(ApiUser user, string token)
        {
            try
            {
                string tokenValidity = await settings.GetSettingValue("api:email:confirmationTokenValidityLifeSpanDays", "2");
                string baseUrl = string.Concat(
                    await settings.GetSettingValue("api:scheme", "https"), "://",
                    await settings.GetSettingValue("api:host", "localhost"), ":",
                    await settings.GetSettingValue("api:port", "5001")
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

                return await emailSending.SendTemplatedEmailAsync(
                    user.Email,
                    "Confirmation d'email",
                    "EmailConfirmation",
                    "fr",
                    parameters
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending confirmation email");
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
                    logger.LogWarning("No user identifier found in token");
                    return null;
                }

                var userByEmail = await userManager.FindByEmailAsync(userEmail);
                if (userByEmail == null)
                {
                    logger.LogWarning($"No user found with email: {userEmail}");
                    return null;
                }
                userId = userByEmail.Id;
            }

            return await View(userId);
        }

        public async Task<bool> ResendConfirmationEmail(string userEmail)
        {
            var user = await userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                logger.LogWarning($"User not found with email: {userEmail}");
                return false;
            }

            if (user.EmailConfirmed)
            {
                logger.LogWarning($"Email already confirmed for user: {userEmail}");
                return false;
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
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
            var users = await userManager.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var userResponses = new List<UserDto>();

            foreach (var user in users)
            {
                // Récupérer explicitement les rôles pour chaque utilisateur
                var roles = await roleService.GetRolesForUser(user.Id);
                var userResponse = MapToVM(user);
                userResponse.Roles = roles; // Utiliser les rôles récupérés via le roleService
                userResponses.Add(userResponse);
            }

            return userResponses;
        }
    }
}
