using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new user
    /// </summary>
    public class ApiUserCreateDto
    {
        /// <summary>
        /// Email address of the new user
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// First name of the new user
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last name of the new user
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// List of role names to be assigned to the new user
        /// </summary>
        public List<string> Roles { get; set; }
    }
}
