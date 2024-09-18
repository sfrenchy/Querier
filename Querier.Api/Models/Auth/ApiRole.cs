using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Querier.Api.Models.Auth
{
    public class ApiRole: IdentityRole
    {
        public ApiRole(): base() {}
        public ApiRole(string name): base(name) {}

        public virtual List<QCategoryRole> QCategoryRoles { get; set; }
        public virtual List<QCardRole> QCardRoles { get; set; }
        public virtual List<QPageRole> QPageRoles { get; set; }
    }
}
