using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.UI
{
    public class HAPage : UIDBEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        [Column("Title")]
        public string Title { get; set; }
        [Column("Description")]
        public string Description { get; set; }
        [Column("Icon")]
        public string Icon { get; set; }
        [Column("HAPageCategoryId")]
        public int? HAPageCategoryId { get; set; }
        [JsonIgnore]
        public virtual HAPageCategory HAPageCategory { get; set; }
        [JsonIgnore]
        public virtual List<HAPageRow> HAPageRows { get; set; }
        [JsonIgnore]
        public virtual List<HAPageRole> HAPageRoles { get; set; }
    }
}
