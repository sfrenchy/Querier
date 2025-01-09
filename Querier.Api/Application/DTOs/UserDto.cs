using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Represents a user's data in the system
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Unique identifier of the user
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// User's last name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// User's phone number
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// List of roles assigned to the user
        /// </summary>
        public List<RoleDto> Roles { get; set; }

        /// <summary>
        /// User's preferred language code
        /// </summary>
        public string LanguageCode { get; set; }

        /// <summary>
        /// URL or path to the user's profile image
        /// </summary>
        public string Img { get; set; }

        /// <summary>
        /// User's position or job title
        /// </summary>
        public string Poste { get; set; }

        /// <summary>
        /// Username for login
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// User's preferred date format
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// User's preferred currency
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// User's preferred area unit
        /// </summary>
        public string AreaUnit { get; set; }

        /// <summary>
        /// Indicates whether the user's email has been confirmed
        /// </summary>
        public bool IsEmailConfirmed { get; set; }
    }
}
