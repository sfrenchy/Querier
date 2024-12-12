using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests.Role;
using Querier.Api.Models.Responses.Role;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Services.Repositories.Role;

namespace Querier.Api.Services.Role
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

        public async Task<bool> Add(RoleRequest role)
        {
            var newRole = new ApiRole(role.Name);
            return await _repoRole.Add(newRole);
        }

        public async Task<bool> Edit(RoleRequest role)
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

        public async Task<List<RoleResponse>> GetAll()
        {
            return (await _repoRole.GetAll()).Select(ug => new RoleResponse { Id = ug.Id, Name = ug.Name }).ToList();
        }

        public async Task<List<RoleResponse>> GetRolesForUser(string idUser)
        {
            var responses = new List<RoleResponse>();
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var listUserRoles = apidbContext.UserRoles.Where(ur => ur.UserId == idUser).ToList();
                foreach (var role in listUserRoles)
                {
                    var elementToAdd = apidbContext.Roles.FirstOrDefault(r => r.Id == role.RoleId);
                    RoleResponse ElementVM = new RoleResponse
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