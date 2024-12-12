using Microsoft.AspNetCore.Identity;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.Auth
{
    public class ApiUserRole : IdentityUserRole<string>
    {
        public virtual ApiUser User { get; set; }
        public virtual ApiRole Role { get; set; }
    }
} 