using System.Collections.Generic;
using System.Linq;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Repositories;

namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object for menu item information
    /// </summary>
    public class MenuDto
    {
        /// <summary>
        /// Unique identifier of the menu item
        /// </summary>
        public int Id { get; set; }

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
        /// Indicates whether the menu item is visible in the navigation
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// List of role names that have access to this menu item
        /// </summary>
        public List<RoleDto> Roles { get; set; }

        /// <summary>
        /// Navigation route for the menu item
        /// </summary>
        public string Route { get; set; }

        public static MenuDto FromEntity(Menu entity)
        {
            var scope = ServiceActivator.GetScope();
            var roleRepository = (IRoleRepository) scope.ServiceProvider.GetService(typeof(IRoleRepository));
            var roles = new List<ApiRole>();
            if (roleRepository != null)
            {
                roles = roleRepository.GetAll();
            }
            return new MenuDto
            {
                Id = entity.Id,
                Title = entity.Translations.Select(x => new TranslatableStringDto() { LanguageCode = x.LanguageCode, Value = x.Name }).ToList(),
                Icon = entity.Icon,
                Order = entity.Order,
                IsVisible = entity.IsVisible,
                Roles = entity.Roles.Split(',').Select(x => new RoleDto()
                {
                    Id = roles.FirstOrDefault(r => r.Name == x)?.Id,
                    Name = roles.FirstOrDefault(r => r.Name == x)?.Name,
                }).ToList(),
                Route = entity.Route
            };
        }
    }
} 