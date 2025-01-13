using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for updating an existing page
    /// </summary>
    public class PageUpdateDto
    {
        /// <summary>
        /// Unique identifier of the page
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Updated icon identifier or class name for the page
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Updated display order of the page in the navigation
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Updated visibility status of the page in the navigation
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Updated array of role names that have access to this page
        /// </summary>
        public RoleDto[] Roles { get; set; }

        /// <summary>
        /// Updated navigation route for the page
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Updated ID of the dynamic menu category this page belongs to
        /// </summary>
        public int MenuId { get; set; }

        /// <summary>
        /// List of translations for the page's title in different languages
        /// </summary>
        public List<TranslatableStringDto> Title { get; set; }
    }
}