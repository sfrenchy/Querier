using System;
using System.Collections.Generic;

namespace Querier.Api.Infrastructure.Database.Templates
{
    public class TemplateProperty
    {
        public string Name { get; set; }
        public string CSName { get; set; }
        public string CSParameterName { get { return char.ToLowerInvariant(CSName[0]) + CSName.Substring(1); } }
        public string CSType { get; set; }
        public bool IsKey { get; set; }
        public bool IsRequired { get; set; }
        public bool IsAutoGenerated { get; set; }
        public bool IsForeignKey { get; set; }
        public bool IsEntityKey => IsKey && !IsForeignKey;
        public bool IsLinqToSqlSupportedType { get; set; }
        public bool IsInt => CSType.Contains("int");
        public bool IsString => CSType.Contains("string");
        public bool IsDateTime => CSType.Contains("DateTime");
        public bool IsNullable => !IsRequired;
        public bool IsDecimal => CSType.Contains("decimal");
        public bool IsGuid => CSType.Contains("Guid");
        public bool IsBool => CSType.Contains("bool");
        public bool IsByte => CSType.Contains("byte");
        public bool IsShort => CSType.Contains("short");
        public bool IsLong => CSType.Contains("long");
        public bool IsFloat => CSType.Contains("float");
        public bool IsDouble => CSType.Contains("double");
        public bool IsTimeSpan => CSType.Contains("TimeSpan");
        public bool IsArray => CSType.Contains("[]");
    }
} 