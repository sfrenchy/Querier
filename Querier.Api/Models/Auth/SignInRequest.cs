using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Models.Auth
{
    public class SignInRequest
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
