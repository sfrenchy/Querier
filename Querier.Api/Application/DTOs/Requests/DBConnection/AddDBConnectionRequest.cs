using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Application.DTOs.Requests.DBConnection
{
    public class AddDBConnectionRequest
    {
        public QDBConnectionType ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string ContextApiRoute { get; set; }
        public bool GenerateProcedureControllersAndServices { get; set; } = true;
    }
} 