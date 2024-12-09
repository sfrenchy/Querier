namespace Querier.Api.Models.Requests
{
    public class SetupRequest
    {
        public AdminSetup Admin { get; set; }
        public SmtpSetup Smtp { get; set; }
    }

    public class AdminSetup
    {
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class SmtpSetup
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool useSSL { get; set; }
        public string SenderEmail { get; set; }
        public string SenderName { get; set; }
    }
} 