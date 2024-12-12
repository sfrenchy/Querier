using System;

namespace Querier.Api.Domain.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DynamicContextProcedureAttribute : Attribute
    {
        public string ContextName { get; set; }
        public string ServiceName { get; set; }
    }
}