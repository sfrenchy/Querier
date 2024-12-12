using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Querier.Api.Application.DTOs.Requests.Auth
{
    public class ResendConfirmationEmailRequest
    {
        public string UserId { get; set; }
    }
}