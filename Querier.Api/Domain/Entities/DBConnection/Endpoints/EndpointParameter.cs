using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Querier.Api.Domain.Entities.QDBConnection.Endpoints
{
    public class EndpointParameter
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EndpointDescriptionId { get; set; }

        [ForeignKey("EndpointDescriptionId")]
        [InverseProperty("Parameters")]
        [DeleteBehavior(DeleteBehavior.Cascade)]
        public virtual EndpointDescription EndpointDescription { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public string Source { get; set; } // FromBody, FromQuery, FromRoute, etc.

        /// <summary>
        /// JSON Schema complet du type (format JSON Schema)
        /// Inclut toutes les informations sur le type et ses validations
        /// Exemple :
        /// {
        ///   "type": "object",
        ///   "required": ["name", "age"],
        ///   "properties": {
        ///     "name": { 
        ///       "type": "string", 
        ///       "minLength": 1,
        ///       "description": "Le nom de l'utilisateur"
        ///     },
        ///     "age": { 
        ///       "type": "integer", 
        ///       "minimum": 0,
        ///       "description": "L'âge de l'utilisateur"
        ///     },
        ///     "email": {
        ///       "type": "string",
        ///       "format": "email",
        ///       "pattern": "^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$"
        ///     },
        ///     "role": {
        ///       "type": "string",
        ///       "enum": ["Admin", "User", "Guest"],
        ///       "default": "User"
        ///     },
        ///     "addresses": {
        ///       "type": "array",
        ///       "items": {
        ///         "type": "object",
        ///         "required": ["street"],
        ///         "properties": {
        ///           "street": { "type": "string" },
        ///           "city": { "type": "string" },
        ///           "zipCode": { 
        ///             "type": "string",
        ///             "pattern": "^\\d{5}$"
        ///           }
        ///         }
        ///       }
        ///     }
        ///   }
        /// }
        /// </summary>
        [Required]
        [Column(TypeName = "json")]
        public string JsonSchema { get; set; }
    }
} 