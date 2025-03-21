using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Domain.Entities
{
    public class SQLQuery
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Query { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public bool IsPublic { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public int ConnectionId { get; set; }
        public virtual DBConnection.DBConnection Connection { get; set; }
        public string OutputDescription { get; set; }
    }
} 