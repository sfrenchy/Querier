using System.Collections.Generic;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Linq;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Repositories;

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
        /// Indicates whether the page is visible in the navigation
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// List of role names that have access to this page
        /// </summary>
        public List<RoleDto> Roles { get; set; }

        /// <summary>
        /// Navigation route for the page
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// ID of the dynamic menu category this page belongs to
        /// </summary>
        public int MenuId { get; set; }

        /// <summary>
        /// List of rows that make up the page's layout
        /// </summary>
        public List<RowDto> Rows { get; set; }

        public static PageDto FromEntity(Page entity)
        {
            var scope = ServiceActivator.GetScope();
            var roleRepository = (IRoleRepository) scope.ServiceProvider.GetService(typeof(IRoleRepository));
            var roles = new List<ApiRole>();
            if (roleRepository != null)
            {
                roles = roleRepository.GetAll();
            }

            return new PageDto
            {
                Id = entity.Id,
                Title = entity.PageTranslations.Select(x => new TranslatableStringDto() { LanguageCode = x.LanguageCode, Value = x.Name }).ToList(),
                Icon = entity.Icon,
                Order = entity.Order,
                IsVisible = entity.IsVisible,
                Route = entity.Route,
                MenuId = entity.MenuId,
                Roles = entity.Roles.Split(',').Select(x => new RoleDto()
                {
                    Id = roles.FirstOrDefault(r => r.Name == x)?.Id,
                    Name = roles.FirstOrDefault(r => r.Name == x)?.Name,
                }).ToList(),
            };
        }
    }
} 