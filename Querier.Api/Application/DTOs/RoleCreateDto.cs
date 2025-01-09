using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new role
    /// </summary>
    public class RoleCreateDto
    {
        /// <summary>
        /// Required name of the new role
        /// </summary>
        [Required]
        public string Name { get; set; }
    }
}
