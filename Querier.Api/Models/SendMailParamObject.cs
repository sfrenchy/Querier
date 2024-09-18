using System.ComponentModel.DataAnnotations;
using Querier.Api.Models.Email;

namespace Querier.Api.Models
{
    public class SendMailParamObject
    {
        [Required]
        public string EmailFrom { get; set; }

        [Required]
        public string EmailTo { get; set; }

        [Required]
        public string bodyEmail { get; set; }

        [Required]
        public string SubjectEmail { get; set; }

        [Required]
        public bool bodyHtmlEmail { get; set; }

        public string CopyEmail { get; set; }
        public ParametersEmail ParameterEmailToFillContent { get; set; }
    }
}
