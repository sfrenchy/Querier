using Querier.Api.Models.UI;

namespace Querier.Api.Models.Auth
{
    public class QCardRole
    {
        public QCardRole() { }

        public QCardRole(string roleId, int cardId, bool view, bool add, bool edit, bool remove)
        {
            ApiRoleId = roleId;
            HAPageCardId = cardId;
            View = view;
            Add = add;
            Edit = edit;
            Remove = remove;
        }

        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Remove { get; set; }

        public string ApiRoleId { get; set; }
        public int HAPageCardId { get; set; }

        public virtual ApiRole ApiRole { get; set; }

        public virtual QPageCard QPageCard { get; set; }
    }
}
