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
    }
}
