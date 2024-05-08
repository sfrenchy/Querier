using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Models
{
    public class ResetPassword
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
    }
}
