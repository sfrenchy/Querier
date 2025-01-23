using System.Collections.Generic;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new menu item
    /// </summary>
    public class MenuCreateDto
    {
        /// <summary>
        /// Dictionary of localized names for the menu item, where key is the language code
        /// </summary>
        public List<TranslatableStringDto> Title { get; set; }

        /// <summary>
        /// Icon identifier or class name for the menu item
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// Display order of the menu item in the navigation
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Indicates whether the menu item should be visible in the navigation
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// List of role names that will have access to this menu item
        /// </summary>
        public List<RoleDto> Roles { get; set; }

        /// <summary>
        /// Navigation route for the menu item
        /// </summary>
        public string Route { get; set; }
    }
} 