using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IRoleService
    {
        Task<List<RoleDto>> GetAll();
        Task<bool> Add(RoleCreateDto role);
        Task<bool> Edit(RoleDto role);
        Task<bool> Delete(string id);
        Task<List<RoleDto>> GetRolesForUser(string idUser);
    }
}