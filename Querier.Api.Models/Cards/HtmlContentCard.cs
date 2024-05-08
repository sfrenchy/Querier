using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Cards
{
    public class AddHtmlContent
    {
        public string LanguageCode { get; set; }
        public int CardId { get; set; }
        public string Content { get; set; }
    }
}
