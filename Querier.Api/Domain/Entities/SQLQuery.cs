using System;
using System.Collections.Generic;

namespace Querier.Api.Domain.Entities
{
    public class SQLQuery
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Query { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public bool IsPublic { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        
        // Ajout de la relation avec QDBConnection
        public int ConnectionId { get; set; }
        public virtual QDBConnection.QDBConnection Connection { get; set; }
        public string OutputDescription { get; set; }
    }
} 