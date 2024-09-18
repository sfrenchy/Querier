using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Querier.Api.Models.Auth;

namespace Querier.Api.Models.Email
{
    public class ParametersEmail
    {
        private IConfiguration _configuration;
        public ParametersEmail(IConfiguration configuration, Dictionary<string, string> keyValues = null, ApiUser user = null)
        {
            this.User = user;
            this.KeyValues = keyValues;
            this._configuration = configuration;
        }
        public ApiUser User { get; private set; }
        public DateTime Date { get { return DateTime.Now; } }
        public Dictionary<string, string> KeyValues { get; private set; }
        public object Endpoints
        {
            get
            {
                return new
                {
                    Api = new
                    {
                        scheme = _configuration.GetSection("Endpoint:Api:scheme"),
                        host = _configuration.GetSection("Endpoint:Api:host"),
                        port = _configuration.GetSection("Endpoint:Api:port"),
                    },
                    Front = new
                    {
                        scheme = _configuration.GetSection("Endpoint:Front:scheme"),
                        host = _configuration.GetSection("Endpoint:Front:host"),
                        port = _configuration.GetSection("Endpoint:Front:port"),
                    }
                };
            }
        }
        public DescriptionVariable DescriptionVariable { get { return new DescriptionVariable(); } }
    }
    public class DescriptionVariable
    {
        public Dictionary<string, string> Description
        {
            get
            {
                return new Dictionary<string, string>
                {
                    ["$User$"] = "description-variable-tamplate-user",
                    ["$Date$"] = "description-variable-tamplate-date",
                    ["$Endpoints$"] = "description-variable-tamplate-endpoints",
                    ["$KeyValues$"] = "description-variable-tamplate-keyValues",
                };
            }

        }
    }
}
