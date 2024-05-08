using Querier.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Querier.Api.Models.Common;

namespace Querier.Api.Services
{
    public interface IEditModeService
    {
        bool GetAuth(List<IdentityRole> userRoles);
        List<IdentityRole> GetRolesForUser(string userId);
    }
    public class EditModeService : IEditModeService
    {
        private readonly ILogger<TranslationService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;

        public EditModeService(ILogger<TranslationService> logger, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public bool GetAuth(List<IdentityRole> userRoles)
        {
            if (userRoles.Count == 0)
                return false;
            // Check if the user have the right/role to access to edit mode
            if (userRoles.Where(role => role.Name == "Admin" || role.Name == "PowerUser").Any())
                return true;
            return false;
        }

        public List<IdentityRole> GetRolesForUser(string userId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var result = apidbContext.UserRoles.Where(role => role.UserId == userId).ToList();

                List<IdentityRole> roles = new List<IdentityRole>();
                foreach (var userRole in result)
                {
                    roles.Add(apidbContext.Roles.Where(role => role.Id == userRole.RoleId).FirstOrDefault());
                }
                return roles;
            }
        }
    }
}
