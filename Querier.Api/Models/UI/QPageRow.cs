using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Querier.Api.Models.UI
{
    public class QPageRow : UIDBEntity
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }
        [Column("Order")]
        public int Order { get; set; }
        [Column("HAPageId")]
        public int HAPageId { get; set; }
        [JsonIgnore]
        public virtual QPage QPage { get; set; }
        [JsonIgnore]
        public virtual List<QPageCard> QPageCards { get; set; }

        public static QPageRow FromHAPageVMRow(QPageRowVM row)
        {
            return new QPageRow
            {
                Id = row.Id,
                Order = row.Order,
                HAPageId = row.HAPageId,
                QPageCards = row.QPageCards
            };
        }
    }
}
