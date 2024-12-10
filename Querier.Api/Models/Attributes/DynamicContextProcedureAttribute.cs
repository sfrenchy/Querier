using System;

namespace Querier.Api.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DynamicContextProcedureAttribute : Attribute
    {
        public string ContextName { get; set; }
        public string ServiceName { get; set; }
    }
}