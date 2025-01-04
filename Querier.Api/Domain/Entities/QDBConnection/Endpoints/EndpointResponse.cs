using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Querier.Api.Domain.Entities.QDBConnection.Endpoints
{
    public class EndpointResponse
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EndpointDescriptionId { get; set; }

        [ForeignKey("EndpointDescriptionId")]
        [InverseProperty("Responses")]
        [DeleteBehavior(DeleteBehavior.Cascade)]
        public virtual EndpointDescription EndpointDescription { get; set; }

        [Required]
        public int StatusCode { get; set; }

        [Required]
        public string Type { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// JSON Schema complet du type de retour (format JSON Schema)
        /// Inclut toutes les informations sur le type et ses validations
        /// Exemple pour un type de retour complexe :
        /// {
        ///   "type": "object",
        ///   "properties": {
        ///     "id": {
        ///       "type": "integer",
        ///       "format": "int64",
        ///       "description": "Identifiant unique"
        ///     },
        ///     "status": {
        ///       "type": "string",
        ///       "enum": ["Pending", "Active", "Completed"],
        ///       "description": "État actuel"
        ///     },
        ///     "data": {
        ///       "type": "array",
        ///       "items": {
        ///         "type": "object",
        ///         "properties": {
        ///           "name": { 
        ///             "type": "string",
        ///             "description": "Nom de l'élément"
        ///           },
        ///           "value": { 
        ///             "type": "number",
        ///             "format": "decimal",
        ///             "minimum": 0,
        ///             "description": "Valeur de l'élément"
        ///           }
        ///         }
        ///       }
        ///     },
        ///     "metadata": {
        ///       "type": "object",
        ///       "additionalProperties": {
        ///         "type": "string"
        ///       },
        ///       "description": "Métadonnées additionnelles"
        ///     },
        ///     "timestamp": {
        ///       "type": "string",
        ///       "format": "date-time",
        ///       "description": "Date et heure de la réponse"
        ///     }
        ///   }
        /// }
        /// 
        /// Exemple pour une réponse d'erreur (400, 404, etc.) :
        /// {
        ///   "type": "object",
        ///   "properties": {
        ///     "code": {
        ///       "type": "string",
        ///       "description": "Code d'erreur"
        ///     },
        ///     "message": {
        ///       "type": "string",
        ///       "description": "Message d'erreur"
        ///     },
        ///     "details": {
        ///       "type": "array",
        ///       "items": {
        ///         "type": "object",
        ///         "properties": {
        ///           "field": {
        ///             "type": "string",
        ///             "description": "Champ concerné par l'erreur"
        ///           },
        ///           "message": {
        ///             "type": "string",
        ///             "description": "Description de l'erreur"
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