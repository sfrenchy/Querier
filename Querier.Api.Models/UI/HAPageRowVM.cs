using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.UI
{
    public class HAPageRowVM
    {
        public int Id { get; set; }
        public int Order { get; set; }
        public int HAPageId { get; set; }
        public List<HAPageCard> HAPageCards { get; set; }

        public static HAPageRowVM FromHAPageRow(HAPageRow row)
        {
            return new HAPageRowVM
            {
                Id = row.Id,
                Order = row.Order,
                HAPageId = row.HAPageId,
                HAPageCards = row.HAPageCards
            };
        }
    }
}
