using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.UI
{
    public class QPageRowVM
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public int HAPageId { get; set; }
        public List<QPageCard> HAPageCards { get; set; }

        public static QPageRowVM FromHAPageRow(QPageRow row)
        {
            return new QPageRowVM
            {
                Id = row.Id,
                Order = row.Order,
                HAPageId = row.HAPageId,
                HAPageCards = row.HAPageCards
            };
        }
    }
}
