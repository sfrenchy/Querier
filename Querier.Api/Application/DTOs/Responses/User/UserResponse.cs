using System.Collections.Generic;
using Querier.Api.Application.DTOs.Responses.Role;

namespace Querier.Api.Application.DTOs.Responses.User
{
    public class UserResponse
    {
        public string Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<RoleResponse> Roles { get; set; }
        public string LanguageCode { get; set; }
        public string Img { get; set; }
        public string Poste { get; set; }
        public string UserName { get; set; }
        public string DateFormat { get; set; }
        public string Currency { get; set; }
        public string AreaUnit { get; set; }
        public bool IsEmailConfirmed { get; set; }
    }
}
