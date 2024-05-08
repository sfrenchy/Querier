using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests.Role;
using Querier.Api.Models.Responses.Role;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Services.Repositories.Role;

namespace Querier.Api.Services.Role
{
    public interface IRoleService : IDisposable
    {
        Task<List<RoleResponse>> GetAll();
        Task<List<CategoryActionsList>> GetCategories();
        Task<bool> Add(RoleRequest role);
        Task<bool> Edit(RoleRequest role);
        Task<bool> Delete(string id);
        Task<bool> UpdateCategories(CategoryActionsList[] actions);
        Task<bool> AddActionsMissing(ActionsMissing actions);
        Task<GetAllRolesAndPagesAndRelationBetweenResponse> GetAllRolesAndPagesAndRelationBetween();
        Task<bool> AddOrRemoveRoleViewOnPage(ModifyRoleViewOnPageRequest request);
        Task<bool> InsertViewPageRole(InsertViewPageRoleRequest request);
        Task<List<RoleResponse>> GetRolesForUser(string idUser);
        Task<List<GetAllPagesWithRolesResponse>> GetAllPagesWithRoles();
        public ApiRole[] UseMapToModel(List<RoleRequest> roles);
    }


    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _repoRole;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
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

        public async Task<List<CategoryActionsList>> GetCategories()
        {
            var categories = (await _repoRole.GetCategories()).Select(c => new CategoryActionsList
            {
                CategoryId = c.Id,
                Name = c.Label,
                Actions = c.HACategoryRoles.Select(v => new CategoryActions(v.ApiRoleId, v.View, v.Add, v.Edit)).ToList(),
                Pages = c.HAPages.Select(p => new PageActionsList
                {
                    PageId = p.Id,
                    Name = p.Title,
                    Actions = p.HAPageRoles.Select(v => new PageCartActions(v.ApiRoleId, v.View, v.Add, v.Edit, v.Remove)).ToList(),
                    Cards = p.HAPageRows.SelectMany(r => r.HAPageCards.Select(c => new CardActionsList
                    {
                        CardId = c.Id,
                        Name = c.Title,
                        Actions = c.HACardRoles.Select(v => new PageCartActions(v.ApiRoleId, v.View, v.Add, v.Edit, v.Remove)).ToList()
                    })).ToList()
                }).ToList()
            }).ToList();

            var groups = await _repoRole.GetAll();

            foreach (var category in categories)
            {
                if (category.Actions.Count < groups.Count)
                {
                    category.Actions.AddRange(RemoveDataFromListActionsCategory(category.Actions, groups));
                }
                foreach (var page in category.Pages)
                {
                    if (page.Actions.Count < groups.Count)
                    {
                        page.Actions.AddRange(RemoveDataFromListActionsPageCard(page.Actions, groups));
                    }
                    foreach (var card in page.Cards)
                    {
                        if (card.Actions.Count < groups.Count)
                        {
                            card.Actions.AddRange(RemoveDataFromListActionsPageCard(page.Actions, groups));
                        }
                    }
                }
            }

            return categories;
        }

        public async Task<bool> UpdateCategories(CategoryActionsList[] actionList)
        {
            var categoriesRolesActions = actionList.SelectMany(actions => actions.Actions
                .Select(v => new HACategoryRole(v.RoleId, actions.CategoryId, v.View, v.Add, v.Edit))).ToList();

            var pagesRolesActions = actionList.SelectMany(actions => actions.Pages
                .SelectMany(p => p.Actions
                .Select(v => new HAPageRole(v.RoleId, p.PageId, v.View, v.Add, v.Edit, v.Remove)))).ToList();

            var cardsRolesActions = actionList.SelectMany(actions => actions.Pages
                .SelectMany(p => p.Cards
                .SelectMany(c => c.Actions
                .Select(v => new HACardRole(v.RoleId, c.CardId, v.View, v.Add, v.Edit, v.Remove))))).ToList();

            return await _repoRole.UpdateCategoryRoleActionsList(categoriesRolesActions)
            && await _repoRole.UpdatePageRoleActionsList(pagesRolesActions)
            && await _repoRole.UpdateCardRoleActionsList(cardsRolesActions);
        }


        private List<CategoryActions> RemoveDataFromListActionsCategory(List<CategoryActions> actionsList, List<ApiRole> groups)
        {
            foreach (var action in actionsList)
            {
                if (groups.Exists(g => g.Id == action.RoleId))
                {
                    var itemToRemove = groups.FirstOrDefault(g => g.Id == action.RoleId);
                    groups.Remove(itemToRemove);
                }
            }

            return groups.Select(g => new CategoryActions(g.Id)).ToList();
        }

        private List<PageCartActions> RemoveDataFromListActionsPageCard(List<PageCartActions> actionsList, List<ApiRole> groups)
        {
            foreach (var action in actionsList)
            {
                if (groups.Exists(g => g.Id == action.RoleId))
                {
                    var itemToRemove = groups.FirstOrDefault(g => g.Id == action.RoleId);
                    groups.Remove(itemToRemove);
                }
            }

            return groups.Select(g => new PageCartActions(g.Id)).ToList();
        }

        public async Task<bool> AddActionsMissing(ActionsMissing actions)
        {
            return await _repoRole.AddActionsMissing(actions);
        }

        public async Task<GetAllRolesAndPagesAndRelationBetweenResponse> GetAllRolesAndPagesAndRelationBetween()
        {
            GetAllRolesAndPagesAndRelationBetweenResponse response = new GetAllRolesAndPagesAndRelationBetweenResponse();
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                response.Pages = await apidbContext.HAPages.ToListAsync();
                response.Roles = await _repoRole.GetAll();
                response.Category = await apidbContext.HAPageCategories.ToListAsync();
                response.PagesRoles = new List<GetPagesRolesRelationsViewModel>();
                List<HAPageRole> PagesRoles = await apidbContext.HAPageRoles.ToListAsync();
                foreach (var PageRole in PagesRoles)
                {
                    GetPagesRolesRelationsViewModel elementToAdd = new GetPagesRolesRelationsViewModel
                    {
                        ApiRoleId = PageRole.ApiRoleId,
                        HAPageId = PageRole.HAPageId,
                        View = PageRole.View
                    };
                    response.PagesRoles.Add(elementToAdd);
                }
            }
            return response;
        }

        public async Task<bool> AddOrRemoveRoleViewOnPage(ModifyRoleViewOnPageRequest request)
        {
            //action = true -> add view and false -> remove view
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageRole relationPageRole = apidbContext.HAPageRoles.FirstOrDefault(r => r.ApiRoleId == request.roleId && r.HAPageId == request.pageId);
                if (relationPageRole == null)
                {
                    //throw new System.NullReferenceException();
                    return false;
                }
                else
                {
                    relationPageRole.View = request.action;
                }
                await apidbContext.SaveChangesAsync();
            }
            return true;
        }

        public async Task<bool> InsertViewPageRole(InsertViewPageRoleRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageRole newInsert = new HAPageRole
                {
                    ApiRoleId = request.roleId,
                    HAPageId = request.pageId,
                    View = true,
                    Add = true,
                    Edit = true,
                    Remove = true
                };
                apidbContext.HAPageRoles.Add(newInsert);
                await apidbContext.SaveChangesAsync();
            }
            return true;
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

        public async Task<List<GetAllPagesWithRolesResponse>> GetAllPagesWithRoles()
        {
            List<GetAllPagesWithRolesResponse> response = new List<GetAllPagesWithRolesResponse>();
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var pages = await apidbContext.HAPages.ToListAsync();
                foreach (var page in pages)
                {
                    HAPage pageFind = await apidbContext.HAPages.FindAsync(page.Id);
                    var pageVM = HAPageVM.FromHAPage(page);
                    GetAllPagesWithRolesResponse element = new GetAllPagesWithRolesResponse
                    {
                        IdPage = page.Id,
                        Roles = pageVM.Roles
                    };
                    response.Add(element);
                }
                return response;
            }
        }

        public ApiRole[] UseMapToModel(List<RoleRequest> roles)
        {
            return MapToModel(roles);
        }

        private ApiRole[] MapToModel(List<RoleRequest> roles)
        {
            List<ApiRole> listRoles = new List<ApiRole>();
            foreach (RoleRequest role in roles)
            {
                ApiRole apiRole = new ApiRole
                {
                    Id = role.Id,
                    Name = role.Name
                };

                if (apiRole != null)
                {
                    listRoles.Add(apiRole);
                }
            }

            return listRoles.ToArray();
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
    }
}