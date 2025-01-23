using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Data.Context;
using Querier.Api.Infrastructure.Data.Repositories;

namespace Querier.Api.Domain.Services.Role
{


    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRepository _userRepository;
        private bool disposedValue;

        public RoleService(IRoleRepository roleRepository, IUserRepository userRepository)
        {
            _roleRepository = roleRepository;
            _userRepository = userRepository;
        }

        public async Task<RoleDto> GetByIdAsync(string id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return null;
            return new RoleDto { Id = role.Id, Name = role.Name };
        }

        public async Task<RoleDto> AddAsync(RoleCreateDto role)
        {
            var newRole = new ApiRole(role.Name);
            var success = await _roleRepository.AddAsync(newRole);
            if (!success) return null;
            return new RoleDto { Id = newRole.Id, Name = newRole.Name };
        }

        public async Task<bool> UpdateAsync(RoleDto role)
        {
            var roleToEdit = new ApiRole();
            roleToEdit.Id = role.Id;
            roleToEdit.Name = role.Name;
            return await _roleRepository.UpdateAsync(roleToEdit);
        }

        public async Task<bool> DeleteByIdAsync(string id)
        {
            return await _roleRepository.DeleteByIdAsync(id);
        }

        public List<RoleDto> GetAll()
        {
            return _roleRepository.GetAll().Select(ug => new RoleDto { Id = ug.Id, Name = ug.Name }).ToList();
        }

        public async Task<List<RoleDto>> GetRolesForUserAsync(string idUser)
        {
            return (await _userRepository.GetByIdAsync(idUser))
                .UserRoles.Select(ur => ur.Role).
                Select(ur => new RoleDto { Id = ur.Id, Name = ur.Name })
                .ToList();
        }
    }
}