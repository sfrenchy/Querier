using System;

namespace Querier.Api.Domain.Entities.Auth
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public TimeSpan ExpiryTimeFrame { get; set; }
    }
}