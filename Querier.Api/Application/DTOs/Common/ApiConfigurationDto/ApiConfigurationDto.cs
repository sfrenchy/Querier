namespace Querier.Api.Models.Responses.Settings
{
    public class ApiConfigurationDto
    {
        public string Scheme { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string AllowedHosts { get; set; }
        public string AllowedOrigins { get; set; }
        public string AllowedMethods { get; set; }
        public string AllowedHeaders { get; set; }
        public int ResetPasswordTokenValidity { get; set; }
        public int EmailConfirmationTokenValidity { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireNonAlphanumeric { get; set; }
        public bool RequireUppercase { get; set; }
        public int RequiredLength { get; set; }
        public int RequiredUniqueChars { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool SmtpUseSSL { get; set; }
        public string SmtpSenderEmail { get; set; }
        public string SmtpSenderName { get; set; }
        public bool SmtpRequireAuth { get; set; }
        public bool RedisEnabled { get; set; }
        public string RedisHost { get; set; }
        public int RedisPort { get; set; }
    }
} 