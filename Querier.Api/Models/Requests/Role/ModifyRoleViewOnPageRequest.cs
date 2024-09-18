namespace Querier.Api.Models.Requests.Role
{
    public class ModifyRoleViewOnPageRequest
    {
        public bool action { get; set; }
        public string roleId { get; set; }
        public int pageId { get; set; }
    }
}
