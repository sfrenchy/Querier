using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for updating an existing user
    /// </summary>
    public class ApiUserUpdateDto
    {
        /// <summary>
        /// Unique identifier of the user to update
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Updated email address for the user
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Updated first name for the user
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Updated last name for the user
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Updated list of role names assigned to the user
        /// </summary>
        public List<RoleDto> Roles { get; set; }
    }
}
