using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class RoleRepository(ILogger<RoleRepository> logger, RoleManager<ApiRole> roleManager)
        : IRoleRepository
    {
        public List<ApiRole> GetAll()
        {
            try
            {
                logger.LogInformation("Retrieving all roles");
                var roles = roleManager.Roles.ToList();
                logger.LogInformation("Retrieved {Count} roles", roles.Count);
                return roles;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all roles");
                return new List<ApiRole>();
            }
        }

        public async Task<bool> AddAsync(ApiRole role)
        {
            try
            {
                if (role == null)
                {
                    logger.LogError("Attempted to add null role");
                    return false;
                }

                logger.LogInformation("Adding new role: {RoleName}", role.Name);
                var foundRole = await roleManager.Roles.FirstOrDefaultAsync(r => 
                    string.Equals(r.Name, role.Name, StringComparison.OrdinalIgnoreCase));

                if (foundRole != null)
                {
                    logger.LogWarning("Role {RoleName} already exists", role.Name);
                    return false;
                }

                var result = await roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to create role {RoleName}. Errors: {Errors}", 
                        role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return false;
                }

                logger.LogInformation("Successfully created role {RoleName}", role.Name);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding role {RoleName}", role?.Name);
                return false;
            }
        }

        public async Task<bool> UpdateAsync(ApiRole role)
        {
            try
            {
                if (role == null)
                {
                    logger.LogError("Attempted to update null role");
                    return false;
                }

                logger.LogInformation("Updating role {RoleId} to name {RoleName}", role.Id, role.Name);
                var foundRole = await roleManager.FindByIdAsync(role.Id);
                if (foundRole == null)
                {
                    logger.LogWarning("Role with ID {RoleId} not found", role.Id);
                    return false;
                }

                foundRole.Name = role.Name;
                var result = await roleManager.UpdateAsync(foundRole);
                
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to update role {RoleId}. Errors: {Errors}", 
                        role.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return false;
                }

                logger.LogInformation("Successfully updated role {RoleId} to name {RoleName}", role.Id, role.Name);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating role {RoleId}", role?.Id);
                return false;
            }
        }

        public async Task<bool> DeleteByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    logger.LogError("Attempted to delete role with null or empty ID");
                    return false;
                }

                logger.LogInformation("Deleting role with ID {RoleId}", id);
                var foundRole = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id);
                if (foundRole == null)
                {
                    logger.LogWarning("Role with ID {RoleId} not found", id);
                    return false;
                }

                var result = await roleManager.DeleteAsync(foundRole);
                if (!result.Succeeded)
                {
                    logger.LogError("Failed to delete role {RoleName}. Errors: {Errors}", 
                        foundRole.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                    return false;
                }

                logger.LogInformation("Successfully deleted role {RoleName}", foundRole.Name);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting role with ID {RoleId}", id);
                return false;
            }
        }

        public async Task<ApiRole> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    logger.LogError("Attempted to get role with null or empty ID");
                    return null;
                }

                logger.LogInformation("Getting role with ID {RoleId}", id);
                var role = await roleManager.FindByIdAsync(id);
                
                if (role == null)
                {
                    logger.LogWarning("Role with ID {RoleId} not found", id);
                    return null;
                }

                logger.LogInformation("Successfully retrieved role {RoleName}", role.Name);
                return role;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting role with ID {RoleId}", id);
                return null;
            }
        }
    }
}
