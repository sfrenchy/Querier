using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services
{
    public class PageService(IPageRepository repository) : IPageService
    {
        public async Task<PageDto> GetByIdAsync(int id)
        {
            var page = await repository.GetByIdAsync(id);
            return page != null ? MapToResponse(page) : null;
        }

        public async Task<IEnumerable<PageDto>> GetAllAsync()
        {
            var pages = await repository.GetAllAsync();
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
                MenuId = request.DynamicMenuCategoryId,
                PageTranslations = request.Names.Select(x => new PageTranslation
                {
                    LanguageCode = x.Key,
                    Name = x.Value
                }).ToList()
            };

            var result = await repository.CreateAsync(page);
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
                MenuId = request.DynamicMenuCategoryId,
                PageTranslations = request.Translations.Select(t => new PageTranslation
                {
                    LanguageCode = t.LanguageCode,
                    Name = t.Name
                }).ToList()
            };

            var updatedPage = await repository.UpdateAsync(id, page);
            return updatedPage != null ? MapToResponse(updatedPage) : null;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await repository.DeleteAsync(id);
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
                DynamicMenuCategoryId = page.MenuId
            };
        }
    }
} 