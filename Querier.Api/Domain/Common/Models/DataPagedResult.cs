using System.Collections.Generic;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Domain.Common.Models
{
    public class DataPagedResult<T>
    {
        public IEnumerable<T> Items { get; }
        public int Total { get; }
        public DataRequestParametersDto RequestParameters { get; }
        public IEnumerable<ForeignKeyDataDto> ForeignKeyData { get; }

        public DataPagedResult(
            IEnumerable<T> items, 
            int total, 
            DataRequestParametersDto requestParameters,
            IEnumerable<ForeignKeyDataDto> foreignKeyData = null)
        {
            Items = items;
            Total = total;
            RequestParameters = requestParameters;
            ForeignKeyData = foreignKeyData ?? new List<ForeignKeyDataDto>();
        }
    }
} 