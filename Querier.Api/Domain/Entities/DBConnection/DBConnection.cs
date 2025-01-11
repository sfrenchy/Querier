using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;

namespace Querier.Api.Domain.Entities.DBConnection
{
    public class DBConnection
    {
        public DBConnection()
        {
            Endpoints = new HashSet<EndpointDescription>();
        }

        [Key]
        public int Id { get; set; }

        public DbConnectionType ConnectionType { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        
        [Required]
        public string ContextName { get; set; }
        public string ApiRoute { get; set; }
        public string Description { get; set; }
        public string AssemblyHash { get; set; }
        
        // Contenu des fichiers d'assembly
        public byte[] AssemblyDll { get; set; }
        public byte[] AssemblyPdb { get; set; }
        public byte[] AssemblySourceZip { get; set; }

        // Description des endpoints générés
        [InverseProperty("DBConnection")]
        public virtual ICollection<EndpointDescription> Endpoints { get; set; }
    }
}