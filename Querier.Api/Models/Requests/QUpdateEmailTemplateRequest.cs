using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Requests
{
    public class QUpdateEmailTemplateRequest
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string TemplateNewContent { get; set; }
    }
}
