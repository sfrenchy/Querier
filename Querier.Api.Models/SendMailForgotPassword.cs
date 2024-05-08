using System.ComponentModel.DataAnnotations;


namespace Querier.Api.Models
{
    public class SendMailForgotPassword
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
