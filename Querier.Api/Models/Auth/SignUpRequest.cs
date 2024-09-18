using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Models.Auth
{
    public class SignUpRequest
    {
        public string Id { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string LanguageCode { get; set; }
        public string Phone { get; set; }
        public string Img { get; set; }
        public string Poste { get; set; }
        public int UserGroupId { get; set; }
        public List<string> Roles { get; set; }
    }
}
