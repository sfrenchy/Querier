namespace Querier.Api.Models.Requests
{
    public class ExportPageRequest
    {
        public int PageId { get; set; }
        public string FileTitle { get; set; }
        public string RequestUserEmail { get; set; }
    }
}
