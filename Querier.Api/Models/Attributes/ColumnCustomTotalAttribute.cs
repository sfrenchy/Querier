using System;

namespace Querier.Api.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ColumnCustomTotalAttribute : Attribute
    {
        public string ForColumn { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DynamicContextProcedureTotalCalculator : Attribute
    {
        public string DynamicContext { get; set; }
        public string Procedure { get; set; }
    }

    
}