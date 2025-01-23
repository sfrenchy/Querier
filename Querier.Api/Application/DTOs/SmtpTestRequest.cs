using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for testing SMTP server configuration
    /// </summary>
    public class SmtpTestDto
    {
        /// <summary>
        /// Required SMTP server hostname or IP address
        /// </summary>
        [Required]
        public string Host { get; set; }

        /// <summary>
        /// Required SMTP server port number (1-65535)
        /// </summary>
        [Required]
        [Range(1, 65535)]
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
        public bool UseSSL { get; set; }

        /// <summary>
        /// Required email address to use as the sender address
        /// </summary>
        [Required]
        [EmailAddress]
        public string SenderEmail { get; set; }

        /// <summary>
        /// Required display name to use for the sender
        /// </summary>
        [Required]
        public string SenderName { get; set; }

        /// <summary>
        /// Indicates whether SMTP authentication is required
        /// </summary>
        public bool RequireAuth { get; set; }
    }
} 