﻿using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Querier.Api.Domain.Entities.Auth
{
    public partial class ApiUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string ThirdName { get; set; }
        public string LastName { get; set; }
        public virtual ICollection<ApiUserRole> UserRoles { get; set; }

        public ApiUser()
        {
            UserRoles = new HashSet<ApiUserRole>();
        }
    }

    public partial class QApiUserAttributes
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public virtual ApiUser User { get; set; }
    }
}