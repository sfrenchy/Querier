using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Auth
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
