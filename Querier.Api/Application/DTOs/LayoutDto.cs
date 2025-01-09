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
        /// Icon identifier or class name for the page
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Dictionary of localized names for the page, where key is the language code
        /// </summary>
        public Dictionary<string, string> Names { get; set; }

        /// <summary>
        /// Indicates whether the page is visible in the navigation
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// List of role names that have access to this page
        /// </summary>
        public List<string> Roles { get; set; }

        /// <summary>
        /// Navigation route for the page
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// List of rows that make up the page layout
        /// </summary>
        public List<RowDto> Rows { get; set; }
    }
} 