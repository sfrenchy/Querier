using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Querier.Api.Models.Requests
{
    public class ResendConfirmationEmailRequest
    {
        public string UserId { get; set; }
    }
} 