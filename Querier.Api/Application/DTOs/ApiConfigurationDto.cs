namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing the complete API configuration settings
    /// </summary>
    public class ApiConfigurationDto
    {
        /// <summary>
        /// HTTP scheme to use (http or https)
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Host name or IP address where the API is hosted
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Port number where the API listens for requests
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Semicolon-separated list of allowed host names
        /// </summary>
        public string AllowedHosts { get; set; }

        /// <summary>
        /// Semicolon-separated list of allowed CORS origins
        /// </summary>
        public string AllowedOrigins { get; set; }

        /// <summary>
        /// Semicolon-separated list of allowed HTTP methods for CORS
        /// </summary>
        public string AllowedMethods { get; set; }

        /// <summary>
        /// Semicolon-separated list of allowed HTTP headers for CORS
        /// </summary>
        public string AllowedHeaders { get; set; }

        /// <summary>
        /// Validity period in minutes for password reset tokens
        /// </summary>
        public int ResetPasswordTokenValidity { get; set; }

        /// <summary>
        /// Validity period in minutes for email confirmation tokens
        /// </summary>
        public int EmailConfirmationTokenValidity { get; set; }

        /// <summary>
        /// Whether passwords must contain at least one digit
        /// </summary>
        public bool RequireDigit { get; set; }

        /// <summary>
        /// Whether passwords must contain at least one lowercase letter
        /// </summary>
        public bool RequireLowercase { get; set; }

        /// <summary>
        /// Whether passwords must contain at least one non-alphanumeric character
        /// </summary>
        public bool RequireNonAlphanumeric { get; set; }

        /// <summary>
        /// Whether passwords must contain at least one uppercase letter
        /// </summary>
        public bool RequireUppercase { get; set; }

        /// <summary>
        /// Minimum required length for passwords
        /// </summary>
        public int RequiredLength { get; set; }

        /// <summary>
        /// Minimum number of unique characters required in passwords
        /// </summary>
        public int RequiredUniqueChars { get; set; }

        /// <summary>
        /// SMTP server hostname or IP address for sending emails
        /// </summary>
        public string SmtpHost { get; set; }

        /// <summary>
        /// SMTP server port number
        /// </summary>
        public int SmtpPort { get; set; }

        /// <summary>
        /// Username for SMTP authentication
        /// </summary>
        public string SmtpUsername { get; set; }

        /// <summary>
        /// Password for SMTP authentication
        /// </summary>
        public string SmtpPassword { get; set; }

        /// <summary>
        /// Whether to use SSL/TLS for SMTP connections
        /// </summary>
        public bool SmtpUseSSL { get; set; }

        /// <summary>
        /// Email address to use as the sender address
        /// </summary>
        public string SmtpSenderEmail { get; set; }

        /// <summary>
        /// Display name to use for the sender
        /// </summary>
        public string SmtpSenderName { get; set; }

        /// <summary>
        /// Whether SMTP authentication is required
        /// </summary>
        public bool SmtpRequireAuth { get; set; }

        /// <summary>
        /// Whether Redis caching is enabled
        /// </summary>
        public bool RedisEnabled { get; set; }

        /// <summary>
        /// Redis server hostname or IP address
        /// </summary>
        public string RedisHost { get; set; }

        /// <summary>
        /// Redis server port number
        /// </summary>
        public int RedisPort { get; set; }
    }
}