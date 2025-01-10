using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Services.Repositories.Role;
using Querier.Api.Infrastructure.Data.Context;

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

        public async Task<bool> Add(RoleCreateDto role)
        {
            var newRole = new ApiRole(role.Name);
            return await _roleRepository.Add(newRole);
        }

        public async Task<bool> Edit(RoleDto role)
        {
            var roleToEdit = new ApiRole();
            roleToEdit.Id = role.Id;
            roleToEdit.Name = role.Name;
            return await _roleRepository.Edit(roleToEdit);
        }

        public async Task<bool> Delete(string id)
        {
            return await _roleRepository.Delete(id);
        }

        public async Task<List<RoleDto>> GetAll()
        {
            return (await _roleRepository.GetAll()).Select(ug => new RoleDto { Id = ug.Id, Name = ug.Name }).ToList();
        }

        public async Task<List<RoleDto>> GetRolesForUser(string idUser)
        {
            return (await _userRepository.GetById(idUser))
                .UserRoles.Select(ur => ur.Role).
                Select(ur => new RoleDto { Id = ur.Id, Name = ur.Name })
                .ToList();
        }
    }
}