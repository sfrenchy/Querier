namespace Querier.Api.Models.Requests
{
    public class EditPageRequest
    {
        public int PageId { get; set; }
        public int CategoryId { get; set; }
        public string PageTitle { get; set; }
        public string PageDescription { get; set; }
        public string PageIcon { get; set; }
    }
}
