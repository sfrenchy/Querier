using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Application.DTOs
{
    public class SignInDto
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
