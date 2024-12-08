using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Email;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Requests.User;
using Querier.Api.Models.Responses.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Web;
using Querier.Api.Services.Repositories.User;
using Querier.Api.Services.Role;
using System.Security.Claims;

namespace Querier.Api.Services.User
{
    public class UserService : IUserService
    {
        private readonly IConfiguration _configuration;
        private readonly Microsoft.EntityFrameworkCore.IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IEmailSendingService _emailSending;
        private readonly ILogger<UserRepository> _logger;
        private readonly IUserRepository _repo;
        private readonly IRoleService _roleService;
        private readonly Models.Interfaces.IQUploadService _uploadService;

        private readonly UserManager<ApiUser> _userManager;
        private readonly ISettingService _settings;

        // private readonly IQPlugin _herdiaApp;
        public UserService(Microsoft.EntityFrameworkCore.IDbContextFactory<ApiDbContext> contextFactory, ISettingService settings, IUserRepository repo, ILogger<UserRepository> logger, UserManager<ApiUser> userManager, IEmailSendingService emailSending, IConfiguration configuration, Models.Interfaces.IQUploadService uploadService, IRoleService roleService/*, IQPlugin herdiaApp*/)
        {
            _repo = repo;
            _logger = logger;
            _userManager = userManager;
            _emailSending = emailSending;
            _configuration = configuration;
            _contextFactory = contextFactory;
            _uploadService = uploadService;
            _roleService = roleService;
            _settings = settings;
        }

        public async Task<bool> Add(UserRequest user)
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
            
            return await _repo.AddRole(newUser, _roleService.UseMapToModel(user.Roles));
        }

        public async Task<bool> Update(UserRequest user)
        {
            var foundUser = await _repo.GetById(user.Id);
            if (foundUser == null)
            {
                _logger.LogError($"User with Id {user.Id} not found");
                return false;
            }
            MapToModel(user, foundUser);
            if (!await _repo.Edit(foundUser))
            {
                return false;
            }
            //_herdiaApp.herdiaAppUserUpdated(user);

            return await _repo.AddRole(foundUser, _roleService.UseMapToModel(user.Roles));
        }

        public async Task<bool> Delete(string id)
        {
            return await _repo.Delete(id);
        }

        public async Task<UserResponse> View(string id)
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

        public async Task<ServerSideResponse<UserResponse>> GetAll(ServerSideRequest datatableRequest)
        {
            var userList = await _repo.GetAll(datatableRequest);

            return new ServerSideResponse<UserResponse>
            {
                draw = datatableRequest.draw,
                recordsFiltered = userList.FilteredCount,
                recordsTotal = userList.TotalCount,
                data = userList.Users.Select(user => MapToVM(user))
                .ToList()
            };
        }

        public async Task<List<UserResponse>> GetAll()
        {
            List<UserResponse> result = new List<UserResponse>();
            var userList = await _repo.GetAll();
            userList.ForEach(user =>
            {
                result.Add(MapToVM(user));
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

        public async Task<object> SendMailForForgotPassword(SendMailForgotPassword user_mail)
        {
            object response;
            var user = await _userManager.FindByEmailAsync(user_mail.Email);
            //check if the user exist or not
            if (user == null)
            {
                response = new { success = false, message = "Email not find, try again" };
                return response;
            }

            //create mail

            //get the body of the mail from the uploaderManager
            //Get the uploadID
            List<QUploadDefinition> resultat;
            //get the name of the template html for email confirmation with the good language:
            string ResetPasswordTemplateName;
            switch (user.LanguageCode)
            {
                case "fr-FR":
                    ResetPasswordTemplateName = _configuration.GetSection("ApplicationSettings:TemplateFile:Email:Fr:ResetPasswordTemplateName").Get<string>();
                    break;
                case "en-GB":
                    ResetPasswordTemplateName = _configuration.GetSection("ApplicationSettings:TemplateFile:Email:En:ResetPasswordTemplateName").Get<string>();
                    break;
                case "de-DE":
                    ResetPasswordTemplateName = _configuration.GetSection("ApplicationSettings:TemplateFile:Email:De:ResetPasswordTemplateName").Get<string>();
                    break;
                default:
                    ResetPasswordTemplateName = _configuration.GetSection("ApplicationSettings:TemplateFile:Email:En:ResetPasswordTemplateName").Get<string>();
                    break;
            }

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                resultat = apidbContext.QUploadDefinitions.Where(t => t.Nature == QUploadNatureEnum.ApplicationEmail && t.FileName == ResetPasswordTemplateName).ToList();
            }

            //Get the content string of the body Email with a stream:
            Stream fileStream = await _uploadService.GetUploadStream(resultat.First().Id);
            byte[] byteArrayFile;
            using (MemoryStream ms = new MemoryStream())
            {
                fileStream.CopyTo(ms);
                byteArrayFile = ms.ToArray();
            }
            string bodyEmail = System.Text.Encoding.UTF8.GetString(byteArrayFile);
            
            string emailFrom = _configuration.GetSection("ApplicationSettings:SMTP:mailFrom").Get<string>();
            var token = _userManager.GeneratePasswordResetTokenAsync(user);

            //create ParametersEmail it will be use for fill the content of the email 
            string tokenTimeValidity = _configuration.GetSection("ApplicationSettings:ResetPasswordTokenValidityLifeSpanMinutes").Get<string>();
            Dictionary<string, string> keyValues = new Dictionary<string, string>
            {
                { "token", HttpUtility.UrlEncode(token.Result) },
                { "tokenTimeValidityInMinutes", tokenTimeValidity }
            };
            ParametersEmail ParamsEmail = new ParametersEmail(_configuration, keyValues, user);

            //send mail
            SendMailParamObject mailObject = new SendMailParamObject() 
            { 
                EmailTo = user.Email, 
                EmailFrom = emailFrom, 
                bodyEmail = bodyEmail, 
                SubjectEmail = "Reset Password", 
                bodyHtmlEmail = true, 
                CopyEmail = "",
                ParameterEmailToFillContent = ParamsEmail 
            };
            response = null;//await _emailSending.SendEmailAsync(mailObject);

            return response;
        }

        public async Task<object> ResetPassword(ResetPassword reset_password_infos)
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
                string[] ArrayErrorsStringResult = new String[errorsArray.Length];
                for (int i = 0; i < errorsArray.Length; i++)
                {
                    ArrayErrorsStringResult[i] = errorsArray[i].Code;
                }


                response = new { success = false, errors = ArrayErrorsStringResult };
                return response;
            }
        }


        public async Task<object> CheckPassword(CheckPassword Checkpassword)
        {
            object response;
            var valid = new PasswordValidator<ApiUser>();
            var result = await valid.ValidateAsync(_userManager, null, Checkpassword.Password);

            if (result.Errors.Count() == 0)
            {
                response = new { success = true };
                return response;
            }
            else
            {
                var errorsArray = result.Errors.ToArray();
                string[] ArrayErrorsStringResult = new String[result.Errors.Count()];
                for (int i = 0; i < result.Errors.Count(); i++)
                {
                    ArrayErrorsStringResult[i] = errorsArray[i].Code;
                }
                response = new { success = false, errors = ArrayErrorsStringResult };
                return response;
            }
        }

        public async Task<bool> EmailConfirmation(EmailConfirmation emailConfirmation)
        {
            string token = Uri.UnescapeDataString(emailConfirmation.Token);
            var user = await _userManager.FindByEmailAsync(emailConfirmation.Email);
            if (user == null)
                return false;
            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public async Task<(bool Succeeded, string Error)> ConfirmEmailAndSetPassword(EmailConfirmationRequest request)
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

                var decodedToken = Uri.UnescapeDataString(request.Token);
                var confirmResult = await _userManager.ConfirmEmailAsync(user, decodedToken);
                if (!confirmResult.Succeeded)
                {
                    return (false, "Le lien de confirmation n'est plus valide.");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);

                if (!passwordResult.Succeeded)
                {
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
                string tokenValidity = await _settings.GetSettingValue("email:confirmationTokenValidityLifeSpanDays", "2");
                string baseUrl = await _settings.GetSettingValue("application:baseUrl", "https://localhost:5001");

                var parameters = new Dictionary<string, string>
                {
                    { "FirstName", user.FirstName },
                    { "LastName", user.LastName },
                    { "Token", Uri.EscapeDataString(token) },
                    { "Email", user.Email },
                    { "TokenValidity", tokenValidity },
                    { "BaseUrl", baseUrl }
                };

                return await _emailSending.SendTemplatedEmailAsync(
                    user.Email,
                    "Confirmation d'email",
                    "EmailConfirmation",
                    user.LanguageCode ?? "fr",
                    parameters
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending confirmation email");
                return false;
            }
        }

        public async Task<UserResponse> GetCurrentUser(ClaimsPrincipal userClaims)
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

        public async Task<bool> ResendConfirmationEmail(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found with ID: {userId}");
                return false;
            }

            if (user.EmailConfirmed)
            {
                _logger.LogWarning($"Email already confirmed for user: {userId}");
                return false;
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            return await SendConfirmationEmail(user, token);
        }

        private void MapToModel(UserRequest user, ApiUser updateUser)
        {
            updateUser.FirstName = user.FirstName;
            updateUser.LastName = user.LastName;
            updateUser.Email = user.Email;
        }

        private ApiUser MapToModel(UserRequest user)
        {
            return new ApiUser
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.UserName
            };
        }

        private UserResponse MapToVM(ApiUser user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                LanguageCode = user.LanguageCode,
                Img = user.Img,
                DateFormat = user.DateFormat,
                UserName = user.UserName,
                IsEmailConfirmed = user.EmailConfirmed
            };
        }
    }
}
