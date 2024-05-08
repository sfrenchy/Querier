using Querier.Api.Models.Enums.Ged;

namespace Querier.Api.Models.Responses.Ged
{
    public class FileDepositResponse
    {
        public int Id { get; set; }
        public bool Enable { get; set; }
        public string Label { get; set; }
        public string Filter { get; set; }
        public TypeFileDepositEnum Type { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public AuthFileDepositEnum Auth { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string RootPath { get; set; }
        public string Tag { get; set; }
        public CapabilitiesEnum Capabilities { get; set; }
    }
}
