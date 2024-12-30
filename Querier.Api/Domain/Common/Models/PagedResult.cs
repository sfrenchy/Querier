using System.Collections.Generic;

namespace Querier.Api.Domain.Common.Models
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; }
        public int Total { get; }

        public PagedResult(IEnumerable<T> items, int total)
        {
            Items = items;
            Total = total;
        }
    }
} 