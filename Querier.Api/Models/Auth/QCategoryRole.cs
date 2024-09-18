using Querier.Api.Models.UI;

namespace Querier.Api.Models.Auth
{
    public class QCategoryRole
    {
        public QCategoryRole()
        {

        }

        public QCategoryRole(string roleId, int categoryId, bool view, bool add, bool edit)
        {
            ApiRoleId = roleId;
            HAPageCategoryId = categoryId;
            View = view;
            Add = add;
            Edit = edit;
        }

        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }

        public string ApiRoleId { get; set; }
        public int HAPageCategoryId { get; set; }

        public virtual ApiRole ApiRole { get; set; }
        public virtual QPageCategory QPageCategory { get; set; }
    }
}
