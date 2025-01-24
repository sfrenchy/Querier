using System.Collections.Generic;

namespace Querier.Api.Domain.Common.Models
{
    public class ForeignKeyIncludeDto
    {
        public string ForeignKey { get; set; }
        public IEnumerable<string> DisplayColumns { get; set; }
        public string DisplayFormat { get; set; }
    }
}