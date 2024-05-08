using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Auth
{
    public class HAPageRole
    {
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Remove { get; set; }

        public string ApiRoleId { get; set; }
        public int HAPageId { get; set; }

        public virtual ApiRole ApiRole { get; set; }
        public virtual HAPage HAPage { get; set; }

        public HAPageRole()
        {

        }

        public HAPageRole(string roleId, int pageId, bool view, bool add, bool edit, bool remove)
        {
            ApiRoleId = roleId;
            HAPageId = pageId;
            View = view;
            Add = add;
            Edit = edit;
            Remove = remove;
        }
    }
}
