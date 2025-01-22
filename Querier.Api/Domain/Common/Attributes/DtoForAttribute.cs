using System;

namespace Querier.Api.Domain.Common.Attributes
{
    public class DtoForAttribute : Attribute
    {
        public string StoredProcedure { get; set; }
        public DtoType DtoType { get; set;  }
        public string Action { get; set; }
        public string EntityType { get; set; }
    }

    public enum DtoType
    {
        InputDto,
        OutputDto
    }
}