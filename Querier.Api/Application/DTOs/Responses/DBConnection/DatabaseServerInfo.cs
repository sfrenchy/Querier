namespace Querier.Api.Application.DTOs.Responses.DBConnection
{
    public class DatabaseServerInfo
    {
        public string ServerName { get; set; }
        public int Port { get; set; }
        public string NetworkProtocol { get; set; } = "TCP";
    }
} 