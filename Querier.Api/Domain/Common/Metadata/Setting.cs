using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Querier.Api.Domain.Common.Metadata
{
    /// <summary>
    /// Represents an application setting in the system
    /// </summary>
    public class Setting
    {
        /// <summary>
        /// Unique identifier for the setting
        /// </summary>
        /// <example>1</example>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int Id { get; set; }

        /// <summary>
        /// The name/key of the setting
        /// </summary>
        /// <example>isConfigured</example>
        [Column("Name")]
        public string Name { get; set; }

        /// <summary>
        /// The value of the setting
        /// </summary>
        /// <example>true</example>
        [Column("Value")]
        public string Value { get; set; }
        /// <summary>
        /// The description of the setting
        /// </summary>
        /// <example>Indicates if the application is configured</example>
        [Column("Description")]
        public string Description { get; set; }

        /// <summary>
        /// The type of the setting
        /// </summary>
        /// <example>boolean</example>
        [Column("Type")]
        public string Type { get; set; }
    }
}