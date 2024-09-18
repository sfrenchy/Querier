using System;
namespace Querier.Api.Models.Requests
{
    public class QUpdateTranslationRequest
    {
        public string Language { get; set; }
        public string Code { get; set; }
        public string Value { get; set; }
    }

    public class HAUpdateGlobalTranslationRequest
    {
        public string Code { get; set; }
        public string EnLabel { get; set; }
        public string FrLabel { get; set; }
        public string DeLabel { get; set; }
        public string Context { get; set; }
    }
}

