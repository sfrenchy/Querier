using Querier.Api.Application.DTOs.Requests.Setup;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for initial application setup configuration
    /// </summary>
    public class SetupDto
    {
        /// <summary>
        /// Administrative user configuration for initial setup
        /// </summary>
        public SetupAdminDto Admin { get; set; }

        /// <summary>
        /// SMTP server configuration for email services
        /// </summary>
        public SetupSmtpDto Smtp { get; set; }
        /// <summary>
        /// Create a sample for the API using
        /// </summary>
        public bool CreateSample { get; set; }
    }
}