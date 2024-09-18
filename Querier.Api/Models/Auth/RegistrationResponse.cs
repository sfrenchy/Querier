using System.Collections.Generic;

namespace Querier.Api.Models.Auth
{
    public class RegistrationResponse : AuthResult
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string LanguageCode { get; set; }
        public List<string> Roles { get; set; }
        public string DateFormat { get; set; }
        public string Currency { get; set; }
        public string AreaUnit { get; set; }
        public string Img { get; set; }
    }
}
