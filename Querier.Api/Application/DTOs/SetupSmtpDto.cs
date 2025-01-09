namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for SMTP server configuration
    /// </summary>
    public class SetupSmtpDto
    {
        /// <summary>
        /// SMTP server hostname or IP address
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// SMTP server port number
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Username for SMTP authentication
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password for SMTP authentication
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Indicates whether to use SSL/TLS for SMTP connection
        /// </summary>
        public bool useSSL { get; set; }

        /// <summary>
        /// Email address to use as the sender address
        /// </summary>
        public string SenderEmail { get; set; }

        /// <summary>
        /// Display name to use for the sender
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// Indicates whether SMTP authentication is required
        /// </summary>
        public bool RequireAuth { get; set; }
    }
}