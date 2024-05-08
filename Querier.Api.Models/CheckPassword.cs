using System.ComponentModel.DataAnnotations;


namespace Querier.Api.Models
{
    public class CheckPassword
    {
        [Required]
        public string Password { get; set; }
    }
}
