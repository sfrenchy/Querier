using System.ComponentModel;

namespace Querier.Api.Models.Requests
{
    public class CardDefinedConfigRequest
    {
        public string Title { get; set; }
        public string CardTypeLabel { get; set; }
        public string PackageLabel { get; set; }
        public dynamic Config { get; set; }

        [DefaultValue("")]
        public string RequestUserEmail { get; set; }

        [DefaultValue("")]
        public string Width { get; set; }

        [DefaultValue("")]
        public string CardTitle { get; set; }
    }
}
