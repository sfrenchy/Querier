using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.UI
{
    public class QPageCategory
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Label")]
        public string Label { get; set; }

        [Column("Description")]
        public string Description { get; set; }

        [Column("Icon")]
        public string Icon { get; set; }

        [JsonIgnore]
        public virtual List<QPage> QPages { get; set; }

        [JsonIgnore]
        public virtual List<QCategoryRole> QCategoryRoles { get; set; }
    }
}
