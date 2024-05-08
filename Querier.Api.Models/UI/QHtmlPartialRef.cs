using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Models.Common;

namespace Querier.Api.Models.UI
{
    public class HAHtmlPartialRef : UIDBEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        [Column("Language")]
        public string Language { get; set; }
        [Column("Zipped")]
        public bool Zipped { get; set; }
        [Column("HAPageCardId")]
        public int HAPageCardId { get; set; }
        [JsonIgnore]
        public virtual HAPageCard HAPageCard { get; set; }
        [Column("HAUploadDefinitionId")]
        public int HAUploadDefinitionId { get; set; }
        [JsonIgnore]
        public virtual QUploadDefinition QUploadDefinition { get; set; }
    }
}
