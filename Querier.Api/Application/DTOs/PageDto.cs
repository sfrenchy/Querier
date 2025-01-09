using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Represents a page in the application's navigation structure
    /// </summary>
    public class PageDto
    {
        /// <summary>
        /// Unique identifier of the page
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Dictionary of localized names for the page, where key is the language code
        /// </summary>
        public Dictionary<string, string> Names { get; set; }

        /// <summary>
        /// Icon identifier or class name for the page
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Display order of the page in the navigation
        /// </summary>
        public int Order { get; set; }

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
        /// ID of the dynamic menu category this page belongs to
        /// </summary>
        public int DynamicMenuCategoryId { get; set; }

        /// <summary>
        /// List of rows that make up the page's layout
        /// </summary>
        public List<RowDto> Rows { get; set; }
    }
} 