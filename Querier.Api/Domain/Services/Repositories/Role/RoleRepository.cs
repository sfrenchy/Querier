using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Responses.Role;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Services.Repositories.User;

namespace Querier.Api.Services.Repositories.Role
{
    public interface IRoleRepository
    {
        Task<List<ApiRole>> GetAll();
        Task<bool> Add(ApiRole role);
        Task<bool> Edit(ApiRole role);
        Task<bool> Delete(string id);
    }

    public class RoleRepository : IRoleRepository
    {
        private readonly ApiDbContext _context;
        private readonly ILogger<UserRepository> _logger;
        private readonly RoleManager<ApiRole> _roleManager;

        public RoleRepository(ApiDbContext context, ILogger<UserRepository> logger, RoleManager<ApiRole> roleManager)
        {
            _context = context;
            _logger = logger;
            _roleManager = roleManager;
        }

        public async Task<List<ApiRole>> GetAll()
        {
            try
            {
                return _roleManager.Roles.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new List<ApiRole>();
            }
        }

        public async Task<bool> Add(ApiRole role)
        {
            try
            {
                var foundRole = await _roleManager.Roles.FirstOrDefaultAsync(r => string.Equals(r.Name, role.Name));
                if (foundRole != null)
                {
                    _logger.LogError($"Role {role.Name} cannot be added because it already exists");
                    return false;
                }

                var created = await _roleManager.CreateAsync(role);
                return created.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public async Task<bool> Edit(ApiRole role)
        {
            try
            {
                var foundRole = await _roleManager.FindByIdAsync(role.Id);
                if (foundRole == null)
                {
                    _logger.LogError($"Role {role.Name} cannot be edited because it's not found");
                    return false;
                }
                foundRole.Name = role.Name;
                return (await _roleManager.UpdateAsync(foundRole)).Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public async Task<bool> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogError("Role with id null or empty cannot be deleted");
                    return false;
                }

                var foundRole = await _roleManager.Roles
                    .FirstOrDefaultAsync(r => r.Id == id);
                if (foundRole == null)
                {
                    _logger.LogError($"Role with id {id} cannot be deleted because it's not found");
                    return false;
                }

                var deleted = await _roleManager.DeleteAsync(foundRole);
                if (!deleted.Succeeded)
                {
                    _logger.LogError($"Error when deleting role {foundRole.Name}");
                    return false;
                }
                return deleted.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }
    }
}
