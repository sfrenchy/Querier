namespace Querier.Api.Models.Requests
{
    public class CreateOrUpdateTranslationRequest
    {
        public string LanguageCode { get; set; }
        public string Context { get; set; }
        public string Code { get; set; }
        public string Value { get; set; }
    }
}
