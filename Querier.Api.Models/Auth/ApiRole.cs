using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Querier.Api.Models.Auth
{
    public class ApiRole: IdentityRole
    {
        public ApiRole(): base() {}
        public ApiRole(string name): base(name) {}

        public virtual List<QCategoryRole> HACategoryRoles { get; set; }
        public virtual List<QCardRole> HACardRoles { get; set; }
        public virtual List<QPageRole> HAPageRoles { get; set; }
    }
}
