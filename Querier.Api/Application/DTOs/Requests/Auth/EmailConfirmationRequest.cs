namespace Querier.Api.Application.DTOs.Requests.Auth
{
    public class EmailConfirmationRequest
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }
}