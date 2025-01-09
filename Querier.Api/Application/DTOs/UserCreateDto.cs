using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new user
    /// </summary>
    public class UserCreateDto
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
        /// Username for the new user's login
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// List of role names to be assigned to the new user
        /// </summary>
        public List<string> Roles { get; set; }
    }
}
