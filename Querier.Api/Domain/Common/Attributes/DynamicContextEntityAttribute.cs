using System;

namespace Querier.Api.Domain.Common.Attributes
{
    /// <summary>
    /// Attribut pour marquer les services d'entités générés dynamiquement
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DynamicContextEntityAttribute : Attribute
    {
        public string ContextName { get; set; }
        public string ServiceName { get; set; }

        public DynamicContextEntityAttribute()
        {
        }
    }
} 