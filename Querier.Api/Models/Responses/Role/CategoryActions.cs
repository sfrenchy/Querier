using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Responses.Role
{
    public class CategoryActions
    {
        public CategoryActions(string roleId, bool view = false, bool add = false, bool edit = false)
        {
            View = view;
            Add = add;
            Edit = edit;
            RoleId = roleId;
        }

        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public string RoleId { get; set; }
    }
}
