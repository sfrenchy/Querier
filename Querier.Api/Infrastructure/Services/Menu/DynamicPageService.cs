using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Menu.Requests;
using Querier.Api.Application.DTOs.Requests.Page;
using Querier.Api.Application.DTOs.Responses.Page;
using Querier.Api.Application.Interfaces.Services.Menu;

namespace Querier.Api.Infrastructure.Services.Menu
{
    public class DynamicPageService : IDynamicPageService
    {
        private readonly IDynamicPageRepository _repository;

        public DynamicPageService(IDynamicPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<PageResponse> GetByIdAsync(int id)
        {
            var page = await _repository.GetByIdAsync(id);
            return page != null ? MapToResponse(page) : null;
        }

        public async Task<IEnumerable<PageResponse>> GetAllAsync()
        {
            var pages = await _repository.GetAllAsync();
            return pages.Select(MapToResponse);
        }

        public async Task<PageResponse> CreateAsync(CreatePageRequest request)
        {
            var page = new DynamicPage
            {
                Icon = request.Icon,
                Order = request.Order,
                IsVisible = request.IsVisible,
                Roles = string.Join(",", request.Roles),
                Route = request.Route,
                DynamicMenuCategoryId = request.DynamicMenuCategoryId,
                DynamicPageTranslations = request.Names.Select(x => new DynamicPageTranslation
                {
                    LanguageCode = x.Key,
                    Name = x.Value
                }).ToList()
            };

            var result = await _repository.CreateAsync(page);
            return MapToResponse(result);
        }

        public async Task<PageResponse> UpdateAsync(int id, UpdateDynamicPageRequest request)
        {
            var page = new DynamicPage
            {
                Icon = request.Icon,
                Order = request.Order,
                IsVisible = request.IsVisible,
                Roles = string.Join(",", request.Roles),
                Route = request.Route,
                DynamicMenuCategoryId = request.DynamicMenuCategoryId,
                DynamicPageTranslations = request.Translations.Select(t => new DynamicPageTranslation
                {
                    LanguageCode = t.LanguageCode,
                    Name = t.Name
                }).ToList()
            };

            var updatedPage = await _repository.UpdateAsync(id, page);
            return updatedPage != null ? MapToResponse(updatedPage) : null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private PageResponse MapToResponse(DynamicPage page)
        {
            return new PageResponse
            {
                Id = page.Id,
                Names = page.DynamicPageTranslations.ToDictionary(x => x.LanguageCode, x => x.Name),
                Icon = page.Icon,
                Order = page.Order,
                IsVisible = page.IsVisible,
                Roles = page.Roles.Split(',').ToList(),
                Route = page.Route,
                DynamicMenuCategoryId = page.DynamicMenuCategoryId
            };
        }
    }
} 