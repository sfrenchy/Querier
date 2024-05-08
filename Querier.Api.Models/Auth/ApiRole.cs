using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Querier.Api.Models.Auth
{
    public class ApiRole: IdentityRole
    {
        public ApiRole(): base() {}
        public ApiRole(string name): base(name) {}

        public virtual List<HACategoryRole> HACategoryRoles { get; set; }
        public virtual List<HACardRole> HACardRoles { get; set; }
        public virtual List<HAPageRole> HAPageRoles { get; set; }
    }
}
