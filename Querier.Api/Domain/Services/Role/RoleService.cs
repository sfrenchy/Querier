using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Services.Repositories.Role;
using Querier.Api.Application.Interfaces.Services.Role;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Domain.Services.Role
{


    public class RoleService : IRoleService
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IRoleRepository _repoRole;
        private bool disposedValue;

        public RoleService(IRoleRepository repo, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _repoRole = repo;
            _contextFactory = contextFactory;
        }

        public async Task<bool> Add(RoleCreateDto role)
        {
            var newRole = new ApiRole(role.Name);
            return await _repoRole.Add(newRole);
        }

        public async Task<bool> Edit(RoleDto role)
        {
            var roleToEdit = new ApiRole();
            roleToEdit.Id = role.Id;
            roleToEdit.Name = role.Name;
            return await _repoRole.Edit(roleToEdit);
        }

        public async Task<bool> Delete(string id)
        {
            return await _repoRole.Delete(id);
        }

        public async Task<List<RoleDto>> GetAll()
        {
            return (await _repoRole.GetAll()).Select(ug => new RoleDto { Id = ug.Id, Name = ug.Name }).ToList();
        }

        public async Task<List<RoleDto>> GetRolesForUser(string idUser)
        {
            var responses = new List<RoleDto>();
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var listUserRoles = apidbContext.UserRoles.Where(ur => ur.UserId == idUser).ToList();
                foreach (var role in listUserRoles)
                {
                    var elementToAdd = apidbContext.Roles.FirstOrDefault(r => r.Id == role.RoleId);
                    RoleDto ElementVM = new RoleDto
                    {
                        Id = elementToAdd.Id,
                        Name = elementToAdd.Name
                    };
                    responses.Add(ElementVM);
                }
                return responses;
            }
        }

        public ApiRole[] UseMapToModel(List<string> roleNames)
        {
            return roleNames.Select(name => new ApiRole { Name = name }).ToArray();
        }

        // // TODO: substituer le finaliseur uniquement si 'Dispose(bool disposing)' a du code pour libérer les ressources non managées
        // ~RoleService()
        // {
        //     // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Ne changez pas ce code. Placez le code de nettoyage dans la méthode 'Dispose(bool disposing)'
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: supprimer l'état managé (objets managés)
                }

                // TODO: libérer les ressources non managées (objets non managés) et substituer le finaliseur
                // TODO: affecter aux grands champs une valeur null
                disposedValue = true;
            }
        }
    }
}