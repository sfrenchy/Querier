namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new row in the page layout
    /// </summary>
    public class RowCreateDto
    {
        /// <summary>
        /// Display order of the row in the layout
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Optional height of the row (in pixels or relative units)
        /// </summary>
        public double? Height { get; set; }
    }
} 