using System.Collections.Generic;
using System.Linq;
using Microsoft.ReportingServices.ReportProcessing.ReportObjectModel;
using Querier.Api.Domain.Entities.Auth;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Represents a user's data in the system
    /// </summary>
    public class ApiUserDto
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
        /// Username for login
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Indicates whether the user's email has been confirmed
        /// </summary>
        public bool IsEmailConfirmed { get; set; }

        public static ApiUserDto FromEntity(ApiUser user)
        {
            return new()
            { 
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                UserName = user.UserName,
                Roles = user.UserRoles.Select(u => u.Role).Select(RoleDto.FromEntity).ToList(),
                IsEmailConfirmed = user.EmailConfirmed
            };
        }

        public static ApiUser ToEntity(ApiUserCreateDto user)
        {
            return new ApiUser()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                UserName = user.Email
            };
        }
    }
}
