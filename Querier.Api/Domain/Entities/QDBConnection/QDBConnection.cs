using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Domain.Entities.QDBConnection
{
    public class QDBConnection
    {
        [Key]
        public int Id { get; set; }

        public QDBConnectionType ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string ContextName { get; set; }
        public string ApiRoute { get; set; }
        public string Description { get; set; }
        public string AssemblyHash { get; set; }
    }

    public class AddDBConnectionRequest
    {
        public QDBConnectionType ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string ContextApiRoute { get; set; }
        public bool GenerateProcedureControllersAndServices { get; set; } = true;
    }

    public class QDBConnectionResponse
    {
        public int Id { get; set; }
        public string ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string ApiRoute { get; set; }
    }
}