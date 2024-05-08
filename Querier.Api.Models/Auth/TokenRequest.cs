using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Models.Auth
{
    public class TokenRequest
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public string RefreshToken { get; set; }
    }
}
