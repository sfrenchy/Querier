using System.Collections.Generic;
using Querier.Api.Models.Auth;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Responses.Role
{
    public class GetAllRolesAndPagesAndRelationBetweenResponse
    {
        public List<ApiRole> Roles { get; set; }
        public List<QPage> Pages { get; set; }
        public List<QPageCategory> Category { get; set; }
        public List<GetPagesRolesRelationsViewModel> PagesRoles { get; set; }
    }
}
