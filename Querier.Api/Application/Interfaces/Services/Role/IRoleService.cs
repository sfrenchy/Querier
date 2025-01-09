using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services.Role
{
    public interface IRoleService : IDisposable
    {
        Task<List<RoleDto>> GetAll();
        Task<bool> Add(RoleCreateDto role);
        Task<bool> Edit(RoleDto role);
        Task<bool> Delete(string id);
        Task<List<RoleDto>> GetRolesForUser(string idUser);
        ApiRole[] UseMapToModel(List<string> roleNames);
    }
}