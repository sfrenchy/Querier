using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Querier.Api.Models.UI
{
    public class HAPageRow : UIDBEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        [Column("Order")]
        public int Order { get; set; }
        [Column("HAPageId")]
        public int HAPageId { get; set; }
        [JsonIgnore]
        public virtual HAPage HAPage { get; set; }
        [JsonIgnore]
        public virtual List<HAPageCard> HAPageCards { get; set; }

        public static HAPageRow FromHAPageVMRow(HAPageRowVM row)
        {
            return new HAPageRow
            {
                Id = row.Id,
                Order = row.Order,
                HAPageId = row.HAPageId,
                HAPageCards = row.HAPageCards
            };
        }
    }
}
