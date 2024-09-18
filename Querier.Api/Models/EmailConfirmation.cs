using System.ComponentModel.DataAnnotations;


namespace Querier.Api.Models
{
    public class EmailConfirmation
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string Email { get; set; }
    }
}
