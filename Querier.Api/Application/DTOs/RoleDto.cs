using Querier.Api.Domain.Entities.Auth;
using StackExchange.Redis;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for role information
    /// </summary>
    public class RoleDto
    {
        /// <summary>
        /// Unique identifier of the role
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the role
        /// </summary>
        public string Name { get; set; }

        public static RoleDto FromEntity(ApiRole role)
        {
            return new RoleDto()
            {
                Id = role.Id,
                Name = role.Name
            };
        }

        public static ApiRole ToEntity(RoleDto role)
        {
            return new ApiRole()
            {
                Id = role.Id,
                Name = role.Name
            };
        }
    }
}
