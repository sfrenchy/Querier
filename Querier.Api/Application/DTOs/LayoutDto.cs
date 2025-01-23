using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for page layout configuration
    /// </summary>
    public class LayoutDto
    {
        /// <summary>
        /// ID of the page this layout belongs to
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// List of rows that make up the page layout
        /// </summary>
        public List<RowDto> Rows { get; set; }
    }
} 