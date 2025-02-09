using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new page
    /// </summary>
    public class PageCreateDto
    {
        /// <summary>
        /// Dictionary of localized names for the page, where key is the language code
        /// </summary>
        public List<TranslatableStringDto> Title { get; set; }

        /// <summary>
        /// Icon identifier or class name for the page
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Display order of the page in the navigation
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Indicates whether the page should be visible in the navigation
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// List of role names that will have access to this page
        /// </summary>
        public List<RoleDto> Roles { get; set; }

        /// <summary>
        /// Navigation route for the page
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// ID of the dynamic menu category this page will belong to
        /// </summary>
        public int MenuId { get; set; }
    }
} 