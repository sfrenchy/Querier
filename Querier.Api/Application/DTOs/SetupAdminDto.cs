namespace Querier.Api.Application.DTOs.Requests.Setup
{
    /// <summary>
    /// Data transfer object for initial administrator account setup
    /// </summary>
    public class SetupAdminDto
    {
        /// <summary>
        /// Last name of the administrator
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// First name of the administrator
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Email address for the administrator account
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Password for the administrator account
        /// </summary>
        public string Password { get; set; }
    }
}