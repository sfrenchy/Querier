using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Responses.Role
{
    public class PageCartActions
    {
        public int Id { get; set; }
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Remove { get; set; }
        public string RoleId { get; set; }

        public PageCartActions(string roleId,  bool view = false, bool add = false, bool edit = false, bool remove = false)
        {
            View = view;
            Add = add;
            Edit = edit;
            Remove = remove;
            RoleId = roleId;
        }
    }
}
