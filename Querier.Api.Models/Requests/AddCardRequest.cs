namespace Querier.Api.Models.Requests
{
    public class AddCardRequest
    {
        public string cardTitle { get; set; }
        public string cardType { get; set; }
        public int cardWidth { get; set; }
        public int pageRowId { get; set; }
        public string package { get; set; }
        public string icon { get; set; }
    }
}
