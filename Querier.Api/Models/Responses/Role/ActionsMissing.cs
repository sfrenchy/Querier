using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Responses.Role
{
    public class ActionsMissing
    {
        public string RoleId { get; set; }
        public string ElementId { get; set; }
        public string Type { get; set; }
        public dynamic Actions { get; set; }
    }
}
