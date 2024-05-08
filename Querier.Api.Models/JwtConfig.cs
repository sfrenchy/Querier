using System;

namespace Querier.Api.Models
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public TimeSpan ExpiryTimeFrame { get { return new TimeSpan(1, 0, 0); } }
    }
}
