using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Querier.Api.Models.Requests.User
{
    public class UserRequest
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public List<string> Roles { get; set; }
    }
}
