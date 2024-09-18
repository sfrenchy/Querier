using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.UI
{
    public class QPage : UIDBEntity
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
        public virtual QPageCategory QPageCategory { get; set; }

        [JsonIgnore]
        public virtual List<QPageRow> QPageRows { get; set; }

        [JsonIgnore]
        public virtual List<QPageRole> QPageRoles { get; set; }
    }
}
