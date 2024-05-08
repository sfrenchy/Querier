using System.Collections.Generic;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Responses
{
    public class PageManagementResponse
    {
        public List<HAPageCategory> Categories { get; set; }
        public List<HAPage> Pages { get; set; }
    }
}
