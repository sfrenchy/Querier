using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Requests.Role;
using Querier.Api.Application.DTOs.Responses.Role;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services.Role
{
    public interface IRoleService : IDisposable
    {
        Task<List<RoleResponse>> GetAll();
        Task<bool> Add(RoleRequest role);
        Task<bool> Edit(RoleRequest role);
        Task<bool> Delete(string id);
        Task<List<RoleResponse>> GetRolesForUser(string idUser);
        ApiRole[] UseMapToModel(List<string> roleNames);
    }
}