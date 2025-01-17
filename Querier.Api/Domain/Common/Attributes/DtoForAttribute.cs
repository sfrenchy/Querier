using System;

namespace Querier.Api.Domain.Common.Attributes
{
    public class DtoForAttribute : Attribute
    {
        public string Action { get; set; }
        public string EntityType { get; set; }
    }
}