using System.Collections.Generic;

namespace Querier.Api.Models.Requests
{
    public class AddPageRequest
    {
        public int CategoryId { get; set; }
        public string PageTitle { get; set; }
        public string PageDescription { get; set; }
        public string PageIcon { get; set; }
        public List<string> IdRoles { get; set; }
    }
}
