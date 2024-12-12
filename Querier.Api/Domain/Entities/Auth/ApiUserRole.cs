using Microsoft.AspNetCore.Identity;

namespace Querier.Api.Domain.Entities.Auth
{
    public class ApiUserRole : IdentityUserRole<string>
    {
        public virtual ApiUser User { get; set; }
        public virtual ApiRole Role { get; set; }
    }
}