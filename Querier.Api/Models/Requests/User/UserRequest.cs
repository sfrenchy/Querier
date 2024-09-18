using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Querier.Api.Models.Requests.Role;

namespace Querier.Api.Models.Requests.User
{
    public class UserRequest
    {
        [Required(AllowEmptyStrings = true)]
        public string Id { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public List<RoleRequest> Roles { get; set; }
        [Required]
        public string UserName { get; set; }
        public string LanguageCode { get; set; }
        public string Phone { get; set; }
        public string Img { get; set; }
        public string DateFormat { get; set; }
    }
}
