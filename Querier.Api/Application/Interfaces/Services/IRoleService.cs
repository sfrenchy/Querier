using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IRoleService
    {
        List<RoleDto> GetAll();
        Task<RoleDto> GetByIdAsync(string id);
        Task<RoleDto> AddAsync(RoleCreateDto role);
        Task<bool> UpdateAsync(RoleDto role);
        Task<bool> DeleteByIdAsync(string id);
        Task<List<RoleDto>> GetRolesForUserAsync(string idUser);
    }
}