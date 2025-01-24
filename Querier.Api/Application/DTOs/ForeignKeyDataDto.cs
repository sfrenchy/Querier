using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    public class ForeignKeyDataDto
    {
        public string ForeignKey { get; set; }
        public IEnumerable<ForeignKeyValueDto> Values { get; set; }
    }
} 