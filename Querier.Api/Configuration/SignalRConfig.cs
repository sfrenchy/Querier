namespace Querier.Api.Configuration
{
    /// <summary>
    /// SignalR configuration options
    /// </summary>
    public class SignalRConfig
    {
        /// <summary>
        /// Maximum size of a SignalR message in bytes
        /// </summary>
        public int MaximumReceiveMessageSize { get; set; } = 102400;

        /// <summary>
        /// Enable detailed error messages (should be false in production)
        /// </summary>
        public bool EnableDetailedErrors { get; set; }

        /// <summary>
        /// Allowed origins for CORS
        /// </summary>
        public string[] AllowedOrigins { get; set; } = new[] { "http://localhost:4200" };
    }
} 