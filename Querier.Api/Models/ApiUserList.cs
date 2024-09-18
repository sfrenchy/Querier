using Querier.Api.Models.Responses;
using System.Collections.Generic;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models
{
    public class ApiUserList
    {
        public List<ApiUser> Users { get; set; }
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
    }
}
