using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Querier.Api.Models.Auth
{
    public class ApiRole : IdentityRole
    {
        public ApiRole() : base()
        {
            UserRoles = new HashSet<ApiUserRole>();
        }

        public ApiRole(string roleName) : base(roleName)
        {
            UserRoles = new HashSet<ApiUserRole>();
        }

        public virtual ICollection<ApiUserRole> UserRoles { get; set; }
    }
}
