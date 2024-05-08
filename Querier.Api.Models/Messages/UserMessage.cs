using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Messages
{
    public class UserMessage
    {
        public string From { get; set; }
        public string To { get; set; }
        public string MessageContent { get; set; }
    }
}
