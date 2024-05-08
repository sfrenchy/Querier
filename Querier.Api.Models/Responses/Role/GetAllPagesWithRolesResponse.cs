using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Responses.Role
{
    public class GetAllPagesWithRolesResponse
    {
        public int IdPage { get; set; }
        public dynamic Roles { get; set; }
    }
}
