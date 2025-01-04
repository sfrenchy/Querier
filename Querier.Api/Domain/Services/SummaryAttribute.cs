using System;

namespace Querier.Api.Domain.Services
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public class SummaryAttribute : Attribute
    {
        public string Summary { get; }

        public SummaryAttribute(string summary)
        {
            Summary = summary;
        }
    }
} 