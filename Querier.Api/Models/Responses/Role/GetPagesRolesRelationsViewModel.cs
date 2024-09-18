using Querier.Api.Models.Auth;
using Querier.Api.Models.UI;
using System.Collections.Generic;

namespace Querier.Api.Models.Responses.Role
{
    public class GetPagesRolesRelationsViewModel
    {
        public string ApiRoleId { get; set; }
        public int HAPageId { get; set; }
        public bool View { get; set; }
    }
}
