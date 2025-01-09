using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services.Menu;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services.Menu
{
    public class DynamicPageService : IDynamicPageService
    {
        private readonly IDynamicPageRepository _repository;

        public DynamicPageService(IDynamicPageRepository repository)
        {
            _repository = repository;
        }

        public async Task<PageDto> GetByIdAsync(int id)
        {
            var page = await _repository.GetByIdAsync(id);
            return page != null ? MapToResponse(page) : null;
        }

        public async Task<IEnumerable<PageDto>> GetAllAsync()
        {
            var pages = await _repository.GetAllAsync();
            return pages.Select(MapToResponse);
        }

        public async Task<PageDto> CreateAsync(PageCreateDto request)
        {
            var page = new Page
            {
                Icon = request.Icon,
                Order = request.Order,
                IsVisible = request.IsVisible,
                Roles = string.Join(",", request.Roles),
                Route = request.Route,
                DynamicMenuCategoryId = request.DynamicMenuCategoryId,
                PageTranslations = request.Names.Select(x => new PageTranslation
                {
                    LanguageCode = x.Key,
                    Name = x.Value
                }).ToList()
            };

            var result = await _repository.CreateAsync(page);
            return MapToResponse(result);
        }

        public async Task<PageDto> UpdateAsync(int id, PageUpdateDto request)
        {
            var page = new Page
            {
                Icon = request.Icon,
                Order = request.Order,
                IsVisible = request.IsVisible,
                Roles = string.Join(",", request.Roles),
                Route = request.Route,
                DynamicMenuCategoryId = request.DynamicMenuCategoryId,
                PageTranslations = request.Translations.Select(t => new PageTranslation
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

        private PageDto MapToResponse(Page page)
        {
            return new PageDto
            {
                Id = page.Id,
                Names = page.PageTranslations.ToDictionary(x => x.LanguageCode, x => x.Name),
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