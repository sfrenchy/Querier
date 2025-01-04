namespace Querier.Api.Application.DTOs.Responses.DBConnection
{
    public class QDBConnectionResponse
    {
        public int Id { get; set; }
        public string ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string ApiRoute { get; set; }
    }
} 