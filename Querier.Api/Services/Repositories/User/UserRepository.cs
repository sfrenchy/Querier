using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Email;
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
        Task<ApiUserList> GetAll(ServerSideRequest datatableRequest);
        Task<List<ApiUser>> GetAll();
        Task<bool> AddRole(ApiUser user, ApiRole[] role);
    }

    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ApiDbContext _context;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IEmailSendingService _emailSending;
        private readonly ILogger<UserRepository> _logger;
        private readonly Models.Interfaces.IQUploadService _uploadService;
        private readonly UserManager<ApiUser> _userManager;

        public UserRepository(ApiDbContext context, UserManager<ApiUser> userManager, ILogger<UserRepository> logger, IConfiguration configuration, IEmailSendingService emailSending, IDbContextFactory<ApiDbContext> contextFactory, Models.Interfaces.IQUploadService uploadService)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _configuration = configuration;
            _emailSending = emailSending;
            _contextFactory = contextFactory;
            _uploadService = uploadService;
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

                string generatedPassword = GenerateRandomPassword();
                var res = await _userManager.CreateAsync(user, generatedPassword);
                if (!res.Succeeded)
                {
                    _logger.LogError($"Erreur lors de l'ajout de l'utilisateur {user.Email}");
                    return false;
                }

                //send mail for email confirmation
                //create mail

                //get the body of the mail from the uploaderManager
                //Get the uploadID
                List<QUploadDefinition> result;
                //get the name of the template html for email confirmation with the good language:
                string EmailConfirmationTemplateName;
                switch (user.LanguageCode)
                {
                    case "fr-FR":
                        EmailConfirmationTemplateName = _configuration.GetSection("ApplicationSettings:TemplateFile:Email:Fr:EmailConfirmationTemplateName").Get<string>();
                        break;
                    case "en-GB":
                        EmailConfirmationTemplateName = _configuration.GetSection("ApplicationSettings:TemplateFile:Email:En:EmailConfirmationTemplateName").Get<string>();
                        break;
                    case "de-DE":
                        EmailConfirmationTemplateName = _configuration.GetSection("ApplicationSettings:TemplateFile:Email:De:EmailConfirmationTemplateName").Get<string>();
                        break;
                    default:
                        EmailConfirmationTemplateName = _configuration.GetSection("ApplicationSettings:TemplateFile:Email:En:EmailConfirmationTemplateName").Get<string>();
                        break;
                }

                using (var apidbContext = _contextFactory.CreateDbContext())
                {
                    result = apidbContext.QUploadDefinitions.Where(t => t.Nature == QUploadNatureEnum.ApplicationEmail && t.FileName == EmailConfirmationTemplateName).ToList();
                }

                //GetUploadStream
                Stream fileStream = await _uploadService.GetUploadStream(result.First().Id);
                byte[] byteArrayFile;
                using (MemoryStream ms = new MemoryStream())
                {
                    fileStream.CopyTo(ms);
                    byteArrayFile = ms.ToArray();
                }
                string bodyEmail = System.Text.Encoding.UTF8.GetString(byteArrayFile);

                string emailFrom = _configuration.GetSection("ApplicationSettings:SMTP:mailFrom").Get<string>();
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                //create ParametersEmail it will be use for fill the content of the email 
                string tokenTimeValidity = _configuration.GetSection("ApplicationSettings:EmailConfirmationTokenValidityLifeSpanDays").Get<string>();
                Dictionary<string, string> keyValues = new Dictionary<string, string>
                {
                    { "token", HttpUtility.UrlEncode(token) },
                    { "tokenTimeValidityInDays", tokenTimeValidity }
                };
                ParametersEmail ParamsEmail = new ParametersEmail(_configuration, keyValues, user);

                //send mail
                SendMailParamObject mailObject = new SendMailParamObject() 
                { 
                    EmailTo = user.Email, 
                    EmailFrom = emailFrom, 
                    bodyEmail = bodyEmail, 
                    SubjectEmail = "email confirmation", 
                    bodyHtmlEmail = true, 
                    CopyEmail = "",
                    ParameterEmailToFillContent = ParamsEmail 
                };
                var response = await _emailSending.SendEmailAsync(mailObject);

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

        public async Task<ApiUserList> GetAll(ServerSideRequest datatableRequest)
        {
            try
            {
                var data = _userManager.Users.ToList().DatatableFilter(datatableRequest, out int? countFiltered).ToList();
                return new ApiUserList
                {
                    Users = data,
                    TotalCount = _userManager.Users.Count(),
                    FilteredCount = countFiltered.Value
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message, null);
                return null;
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

        private string GenerateRandomPassword()
        {
            // A mon avis, on peut passer par ici: var valid = new PasswordValidator<ApiUser>();
            var opts = new PasswordOptions()
            {
                RequireDigit = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequireDigit").Get<bool>(),
                RequireLowercase = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequireLowercase").Get<bool>(),
                RequireNonAlphanumeric = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequireNonAlphanumeric").Get<bool>(),
                RequireUppercase = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequireUppercase").Get<bool>(),
                RequiredLength = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequiredLength").Get<int>(),
                RequiredUniqueChars = _configuration.GetSection("ApplicationSettings:AuthenticationPasswordRules:RequiredUniqueChars").Get<int>()
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
