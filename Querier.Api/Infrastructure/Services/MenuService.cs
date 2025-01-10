using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services
{
    public class MenuService(IMenuRepository repository, ILogger<MenuService> logger) : IMenuService
    {
        private readonly ILogger<MenuService> _logger = logger;

        public async Task<List<MenuDto>> GetAllAsync()
        {
            var categories = await repository.GetAllAsync();
            return categories.Select(MapToResponse).ToList();
        }

        public async Task<MenuDto> GetByIdAsync(int id)
        {
            var category = await repository.GetByIdAsync(id);
            return category != null ? MapToResponse(category) : null;
        }

        public async Task<MenuDto> CreateAsync(MenuCreateDto request)
        {
            var category = new Domain.Entities.Menu.Menu
            {
                Icon = request.Icon,
                Order = request.Order,
                IsVisible = request.IsVisible,
                Roles = string.Join(",", request.Roles),
                Route = request.Route,
                Translations = request.Names.Select(x => new MenuTranslation
                {
                    LanguageCode = x.Key,
                    Name = x.Value
                }).ToList()
            };

            var result = await repository.CreateAsync(category);
            return MapToResponse(result);
        }

        public async Task<MenuDto> UpdateAsync(int id, MenuCreateDto request)
        {
            var category = await repository.GetByIdAsync(id);
            if (category == null) return null;

            category.Icon = request.Icon;
            category.Order = request.Order;
            category.IsVisible = request.IsVisible;
            category.Roles = string.Join(",", request.Roles);
            category.Route = request.Route;

            // Mise à jour des traductions
            category.Translations.Clear();
            foreach (var translation in request.Names)
            {
                category.Translations.Add(new MenuTranslation
                {
                    LanguageCode = translation.Key,
                    Name = translation.Value
                });
            }

            var result = await repository.UpdateAsync(category);
            return MapToResponse(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await repository.DeleteAsync(id);
        }

        private static MenuDto MapToResponse(Domain.Entities.Menu.Menu category)
        {
            return new MenuDto
            {
                Id = category.Id,
                Names = category.Translations.ToDictionary(x => x.LanguageCode, x => x.Name),
                Icon = category.Icon,
                Order = category.Order,
                IsVisible = category.IsVisible,
                Roles = category.Roles?.Split(',').ToList() ?? new List<string>(),
                Route = category.Route
            };
        }
    }
} 