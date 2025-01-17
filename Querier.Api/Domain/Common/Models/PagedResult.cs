using System.Collections.Generic;

namespace Querier.Api.Domain.Common.Models
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; }
        public int Total { get; }
        public DataRequestParametersDto RequestParameters { get; }

        public PagedResult(IEnumerable<T> items, int total, DataRequestParametersDto requestParameters)
        {
            Items = items;
            Total = total;
            RequestParameters = requestParameters;
        }
    }
} 