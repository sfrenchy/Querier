using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Models.Common;
using Querier.Api.Models.Enums;

namespace Querier.Api.Models.QDBConnection
{
    public class QDBConnection
    {
        [Key]
        public int Id { get;set; }
        public QDBConnectionType ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string ApiRoute { get; set; }
        public string Description { get; set; }
        public int AssemblyUploadDefinitionId { get; set; }
        [ForeignKey("AssemblyUploadDefinitionId")]
        public virtual QUploadDefinition AssemblyUploadDefinition { get; set; }
        public int PDBUploadDefinitionId { get; set; }
        [ForeignKey("PDBUploadDefinitionId")]
        public virtual QUploadDefinition PDBUploadDefinition { get; set; }
        public int SourcesUploadDefinitionId { get; set; }
        [ForeignKey("SourcesUploadDefinitionId")]
        public virtual QUploadDefinition SourcesUploadDefinition { get; set; }
    }

    public class AddDBConnectionRequest
    {
        public QDBConnectionType ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string ContextApiRoute { get; set; }
        public bool GenerateProcedureControllersAndServices { get; set;} = true;
    }

    public class QDBConnectionResponse
    {
        public int Id { get;set; }
        public string ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string ApiRoute { get; set; }
        public int AssemblyUploadDefinitionId { get; set; }
        public int PDBUploadDefinitionId { get; set; }
        public int SourcesUploadDefinitionId { get; set; }
    }
}