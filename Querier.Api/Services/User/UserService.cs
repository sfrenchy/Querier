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
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests.User;
using Querier.Api.Models.Responses.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Web;
using Querier.Api.Services.Repositories.Application;
using Querier.Api.Services.Repositories.User;
using Querier.Api.Services.Role;

namespace Querier.Api.Services.User
{
    public interface IUserService
    {
        public Task<UserResponse> View(string id);
        public Task<bool> Add(UserRequest user);
        public Task<bool> Update(UserRequest user);
        public Task<bool> Delete(string id);
        public Task<string> GetPasswordHash(string idUser);
        public Task<ServerSideResponse<UserResponse>> GetAll(ServerSideRequest datatableRequest);
        public Task<List<UserResponse>> GetAll();
        public Task<object> SendMailForForgotPassword(SendMailForgotPassword user_mail);
        public Task<object> ResetPassword(ResetPassword reset_password_infos);
        public Task<object> CheckPassword(CheckPassword Checkpassword);
        public Task<bool> EmailConfirmation(EmailConfirmation emailConfirmation);
    }
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly ILogger<UserRepository> _logger;
        private readonly UserManager<ApiUser> _userManager;
        private readonly IEmailSendingService _emailSending;
        private readonly IConfiguration _configuration;
        private readonly Microsoft.EntityFrameworkCore.IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IQUploadService _uploadService;
        private readonly IRoleService _roleService;
        // private readonly IQPlugin _herdiaApp;
        public UserService(Microsoft.EntityFrameworkCore.IDbContextFactory<ApiDbContext> contextFactory, IUserRepository repo, ILogger<UserRepository> logger, UserManager<ApiUser> userManager, IEmailSendingService emailSending, IConfiguration configuration, IQUploadService uploadService, IRoleService roleService/*, IQPlugin herdiaApp*/)
        {
            _repo = repo;
            _logger = logger;
            _userManager = userManager;
            _emailSending = emailSending;
            _configuration = configuration;
            _contextFactory = contextFactory;
            _uploadService = uploadService;
            _roleService = roleService;
            // _herdiaApp = herdiaApp;
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
            // _herdiaApp.herdiaAppUserCreated(newUser);

            //If the application needs some, create the application-specific user attributes in HAAPIDB
            if (Features.ApplicationUserAttributes.Count > 0)
            {
                //Get Application specific user attributes viewmodels
                List<QEntityAttributeViewModel> ApplicationSpecificUserAttributesViewModels = Features.ApplicationUserAttributes;

                using (var apiDbContext = _contextFactory.CreateDbContext())
                {
                    //Map viewmodel user attributes
                    List<QEntityAttribute> ApplicationSpecificUserAttributes = new List<QEntityAttribute>();

                    ApplicationSpecificUserAttributesViewModels.ForEach(vm =>
                    {
                        QEntityAttribute EntityAttribute = new QEntityAttribute() { Label = vm.Label, Value = vm.Value, Nullable = vm.Nullable };
                        ApplicationSpecificUserAttributes.Add(EntityAttribute);
                    });

                    //Add Application specific attributes to the QEntityAttribute tables
                    apiDbContext.QEntityAttribute.AddRange(ApplicationSpecificUserAttributes);
                    apiDbContext.SaveChanges();

                    //Create a list of QApiUserAttributes
                    List<QApiUserAttributes> userAttributes = new List<QApiUserAttributes>();

                    //For each specific user attributes, create a QApiUserAttributes to link the attribute to the user being created
                    ApplicationSpecificUserAttributes.ForEach(att =>
                    {
                        QApiUserAttributes userAttribute = new QApiUserAttributes() { EntityAttributeId = att.Id, UserId = newUser.Id };
                        userAttributes.Add(userAttribute);
                    });
                    apiDbContext.QApiUserAttributes.AddRange(userAttributes);
                    await apiDbContext.SaveChangesAsync();
                }
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
            //_herdiaApp.herdiaAppUserDeleted(id);
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

        private void MapToModel(UserRequest user, ApiUser updateUser)
        {
            updateUser.FirstName = user.FirstName;
            updateUser.LastName = user.LastName;
            updateUser.Email = user.Email;
            updateUser.LanguageCode = user.LanguageCode;
            updateUser.Phone = user.Phone;
            updateUser.Img = user.Img;
            updateUser.DateFormat = user.DateFormat;
            updateUser.UserName = user.UserName;
        }

        private ApiUser MapToModel(UserRequest user)
        {
            return new ApiUser
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                LanguageCode = user.LanguageCode,
                Phone = user.Phone,
                Img = user.Img,
                DateFormat = user.DateFormat,
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
                UserName = user.UserName
            };
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
            response = await _emailSending.SendEmailAsync(mailObject);

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
    }
}
