using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs.Menu.Requests;
using Querier.Api.Application.DTOs.Menu.Responses;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Application.Interfaces.Services.Menu;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services.Menu
{
    public class MenuCategoryService : IMenuCategoryService
    {
        private readonly IMenuCategoryRepository _repository;
        private readonly ILogger<MenuCategoryService> _logger;

        public MenuCategoryService(IMenuCategoryRepository repository, ILogger<MenuCategoryService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<MenuCategoryResponse>> GetAllAsync()
        {
            var categories = await _repository.GetAllAsync();
            return categories.Select(MapToResponse).ToList();
        }

        public async Task<MenuCategoryResponse> GetByIdAsync(int id)
        {
            var category = await _repository.GetByIdAsync(id);
            return category != null ? MapToResponse(category) : null;
        }

        public async Task<MenuCategoryResponse> CreateAsync(CreateMenuCategoryRequest request)
        {
            var category = new MenuCategory
            {
                Icon = request.Icon,
                Order = request.Order,
                IsVisible = request.IsVisible,
                Roles = string.Join(",", request.Roles),
                Route = request.Route,
                Translations = request.Names.Select(x => new MenuCategoryTranslation
                {
                    LanguageCode = x.Key,
                    Name = x.Value
                }).ToList()
            };

            var result = await _repository.CreateAsync(category);
            return MapToResponse(result);
        }

        public async Task<MenuCategoryResponse> UpdateAsync(int id, CreateMenuCategoryRequest request)
        {
            var category = await _repository.GetByIdAsync(id);
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
                category.Translations.Add(new MenuCategoryTranslation
                {
                    LanguageCode = translation.Key,
                    Name = translation.Value
                });
            }

            var result = await _repository.UpdateAsync(category);
            return MapToResponse(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private static MenuCategoryResponse MapToResponse(MenuCategory category)
        {
            return new MenuCategoryResponse
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