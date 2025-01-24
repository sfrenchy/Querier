using System.Collections.Generic;

namespace Querier.Api.Domain.Common.Models
{
    public class ForeignKeyIncludeConfig
    {
        public string ForeignKey { get; set; }
        public string DisplayFormat { get; set; }
        public IEnumerable<string> DisplayColumns { get; set; }
    }
} 