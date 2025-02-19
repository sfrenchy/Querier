using System.Collections.Generic;

namespace Querier.Api.Domain.Common.Models
{
    public class DataRequestParametersDto
    {
        private const int MaxPageSize = 50;
        private int _pageSize = 10;

        public int PageNumber { get; set; } = 1;
        
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        // Sorting
        public List<OrderByParameterDto> OrderBy { get; set; } = new();

        // Search
        public string GlobalSearch { get; set; }
        public List<ColumnSearchDto> ColumnSearches { get; set; } = new();

        // Foreign Keys
        public List<ForeignKeyIncludeDto> Includes { get; set; } = new();
    }
} 