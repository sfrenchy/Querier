using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Responses.Role
{
    public class CardActionsList
    {
        public int CardId { get; set; }
        public string Name { get; set; }
        public List<PageCartActions> Actions { get; set; }
    }
}
