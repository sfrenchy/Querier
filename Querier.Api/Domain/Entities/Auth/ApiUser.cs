using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Querier.Api.Domain.Entities.Auth
{
    public partial class ApiUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public virtual ICollection<ApiUserRole> UserRoles { get; set; } = new HashSet<ApiUserRole>();
    }
}