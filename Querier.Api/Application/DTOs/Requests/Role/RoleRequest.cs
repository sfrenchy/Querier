using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Application.DTOs.Requests.Role
{
    public class RoleRequest
    {
        [Required(AllowEmptyStrings = true)]
        public string Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
