using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Querier.Api.Models.Attributes;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.UI
{
    public class QPageCard : UIDBEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        [Column("Title")]
        public string Title { get; set; }
        [Column("Width")]
        public int Width { get; set; }
        [Column("CardTypeLabel")]
        [Required]
        public string CardTypeLabel { get; set; }
        [Column("Package")]
        [Required]
        public string Package { get; set; }
        [Column("CardConfiguration")]
        [Required]
        [JsonString]
        public string CardConfiguration { get; set; }

        [NotMapped]
        public dynamic Configuration
        {
            get => JsonConvert.DeserializeObject<dynamic>(CardConfiguration ?? "");
            set => CardConfiguration = JsonConvert.SerializeObject(value);
        }
        [Column("HAPageRowId")]
        public int HAPageRowId { get; set; }
        [Column("Order")]
        public int Order { get; set; }
        [JsonIgnore]
        public virtual HAPageRow HAPageRow { get; set; }
        [JsonIgnore]
        public virtual List<QCardRole> HACardRoles { get; set; }

        public override string ToString()
        {
            return $"{Id} - {CardTypeLabel} - {Title}";
        }
    }
}
