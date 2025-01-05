using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Application.DTOs.Requests.Smtp
{
    public class SmtpTestRequest
    {
        [Required]
        public string Host { get; set; }

        [Required]
        [Range(1, 65535)]
        public int Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public bool UseSSL { get; set; }

        [Required]
        [EmailAddress]
        public string SenderEmail { get; set; }

        [Required]
        public string SenderName { get; set; }

        public bool RequireAuth { get; set; }
    }
} 