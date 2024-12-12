using System.ComponentModel.DataAnnotations;

namespace Querier.Application.DTOs.Auth.Email
{
    public class EmailConfirmation
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string Email { get; set; }
    }
}
