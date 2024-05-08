using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Responses.Role;
using Querier.Api.Models.UI;
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
        Task<List<QPageCategory>> GetCategories();
        Task<bool> UpdateCategoryRoleActionsList(List<QCategoryRole> actions);
        Task<bool> UpdatePageRoleActionsList(List<QPageRole> actions);
        Task<bool> UpdateCardRoleActionsList(List<QCardRole> actions);
        Task<bool> AddActionsMissing(ActionsMissing actions);
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
                    .Include(r => r.HACategoryRoles)
                    .Include(r => r.HAPageRoles)
                    .Include(r => r.HACardRoles)
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

        public async Task<List<QPageCategory>> GetCategories()
        {
            try
            {
                return _context.HAPageCategories
                    .Include(c => c.HACategoryRoles)
                    .Include(c => c.HAPages).ThenInclude(p => p.HAPageRoles)
                    .Include(c => c.HAPages).ThenInclude(p => p.HAPageRows).ThenInclude(r => r.HAPageCards)
                    .Include(c => c.HAPages).ThenInclude(p => p.HAPageRows).ThenInclude(r => r.HAPageCards).ThenInclude(c => c.HACardRoles).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new List<QPageCategory>();
            }
        }

        public async Task<bool> UpdateCategoryRoleActionsList(List<QCategoryRole> actions)
        {
            try
            {
                if (actions == null)
                {
                    _logger.LogError("The list of category's action is null");
                    return false;
                }
                foreach (var action in actions)
                {
                    var foundAction = await _context.HACategoryRoles
                        .FirstOrDefaultAsync(a => a.ApiRoleId == action.ApiRoleId && a.HAPageCategoryId == action.HAPageCategoryId);
                    if (foundAction != null)
                    {
                        foundAction.View = action.View;
                        foundAction.Edit = action.Edit;
                        foundAction.Add = action.Add;
                    }
                    else
                    {
                        await _context.HACategoryRoles.AddAsync(action);
                    }
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdatePageRoleActionsList(List<QPageRole> actions)
        {
            try
            {
                if (actions == null)
                {
                    _logger.LogError("The list of page's action is null");
                    return false;
                }
                foreach (var action in actions)
                {
                    var foundAction = await _context.HAPageRoles
                        .FirstOrDefaultAsync(a => a.ApiRoleId == action.ApiRoleId && a.HAPageId == action.HAPageId);
                    if (foundAction != null)
                    {
                        foundAction.View = action.View;
                        foundAction.Edit = action.Edit;
                        foundAction.Add = action.Add;
                        foundAction.Remove = action.Remove;
                    }
                    else
                    {
                        await _context.HAPageRoles.AddAsync(action);
                    }
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public async Task<bool> UpdateCardRoleActionsList(List<QCardRole> actions)
        {
            try
            {
                if (actions == null)
                {
                    _logger.LogError("The list of card's action is null");
                    return false;
                }
                foreach (var action in actions)
                {
                    var foundAction = await _context.HACardRoles
                        .FirstOrDefaultAsync(a => a.ApiRoleId == action.ApiRoleId && a.HAPageCardId == action.HAPageCardId);
                    if (foundAction != null)
                    {
                        foundAction.View = action.View;
                        foundAction.Edit = action.Edit;
                        foundAction.Add = action.Add;
                        foundAction.Remove = action.Remove;
                    }
                    else
                    {
                        await _context.HACardRoles.AddAsync(action);
                    }
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public async Task<bool> AddActionsMissing(ActionsMissing actions)
        {
            try
            {
                switch (actions.Type)
                {
                    case "CategoryId":
                        await _context.HACategoryRoles.AddAsync(new QCategoryRole(actions.RoleId, int.Parse(actions.ElementId), actions.Actions.View, actions.Actions.Add, actions.Actions.Edit));
                        break;
                    case "PageId":
                        await _context.HAPageRoles.AddAsync(new QPageRole(actions.RoleId, int.Parse(actions.ElementId), actions.Actions.View, actions.Actions.Add, actions.Actions.Edit, actions.Actions.Remove));
                        break;
                    case "CardId":
                        await _context.HACardRoles.AddAsync(new QCardRole(actions.RoleId, int.Parse(actions.ElementId), actions.Actions.View, actions.Actions.Add, actions.Actions.Edit, actions.Actions.Remove));
                        break;
                    default:
                        break;
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }
    }
}
