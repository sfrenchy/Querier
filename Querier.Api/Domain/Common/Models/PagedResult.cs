using System.Collections.Generic;

namespace Querier.Api.Domain.Common.Models
{
    public class PagedResult<T>(IEnumerable<T> items, int total)
    {
        public IEnumerable<T> Items { get; } = items;
        public int Total { get; } = total;
    }
} 