using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Querier.Api.Domain.Entities.QDBConnection.Endpoints
{
    public class EndpointDescription
    {
        public EndpointDescription()
        {
            Parameters = new HashSet<EndpointParameter>();
            Responses = new HashSet<EndpointResponse>();
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public int QDBConnectionId { get; set; }

        [ForeignKey("QDBConnectionId")]
        [InverseProperty("Endpoints")]
        [DeleteBehavior(DeleteBehavior.Cascade)]
        public virtual QDBConnection QDBConnection { get; set; }

        [Required]
        public string Controller { get; set; }

        [Required]
        public string Action { get; set; }

        [Required]
        public string HttpMethod { get; set; }

        [Required]
        public string Route { get; set; }

        public string Description { get; set; }

        [InverseProperty("EndpointDescription")]
        public virtual ICollection<EndpointParameter> Parameters { get; set; }

        [InverseProperty("EndpointDescription")]
        public virtual ICollection<EndpointResponse> Responses { get; set; }
    }
} 