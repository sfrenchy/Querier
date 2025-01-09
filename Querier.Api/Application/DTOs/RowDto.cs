using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for a row in the page layout
    /// </summary>
    public class RowDto
    {
        /// <summary>
        /// Unique identifier of the row
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Display order of the row in the layout
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Optional height of the row (in pixels or relative units)
        /// </summary>
        public double? Height { get; set; }

        /// <summary>
        /// List of cards contained in this row
        /// </summary>
        public List<CardDto> Cards { get; set; }
    }
}