using System;

namespace Querier.Api.Domain.Common.Attributes
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