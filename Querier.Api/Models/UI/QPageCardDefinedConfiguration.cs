using Querier.Api.Models.Common;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Querier.Api.Models.UI
{
    public class QPageCardDefinedConfiguration
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Title")]
        [Required]
        public string Title { get; set; }

        [Column("CardConfiguration")]
        [Required]
        public string CardConfiguration { get; set; }

        [NotMapped]
        public dynamic Configuration
        {
            get
            {
                return JsonConvert.DeserializeObject<dynamic>(CardConfiguration);
            }
            set
            {
                CardConfiguration = JsonConvert.SerializeObject(value);
            }
        }

        [Column("CardTypeLabel")]
        [Required]
        public string CardTypeLabel { get; set; }

        [Column("PackageLabel")]
        [Required]
        public string PackageLabel { get; set; }
    }
}
