using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Querier.Api.Models.Auth
{
    public class ApiRole : IdentityRole
    {
        public string Discriminator { get; set; } = "ApiRole";

        public ApiRole() : base()
        {
        }

        public ApiRole(string roleName) : base(roleName)
        {
            Discriminator = "ApiRole";
        }

        public virtual ICollection<QCategoryRole> QCategoryRoles { get; set; }
        public virtual ICollection<QPageRole> QPageRoles { get; set; }
        public virtual ICollection<QCardRole> QCardRoles { get; set; }
    }
}
