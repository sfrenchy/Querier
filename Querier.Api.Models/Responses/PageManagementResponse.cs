using System.Collections.Generic;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Responses
{
    public class PageManagementResponse
    {
        public List<QPageCategory> Categories { get; set; }
        public List<QPage> Pages { get; set; }
    }
}
