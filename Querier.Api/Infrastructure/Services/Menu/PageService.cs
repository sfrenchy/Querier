using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Requests.Page;
using Querier.Api.Application.DTOs.Responses.Page;
using Querier.Api.Application.Interfaces.Repositories.Menu;
using Querier.Api.Application.Interfaces.Services.Menu;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Infrastructure.Services.Menu
{
    public class PageService : IPageService
    {
        private readonly IPageRepository _repository;

        public PageService(IPageRepository repository)
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
            var page = new Page
            {
                Icon = request.Icon,
                Order = request.Order,
                IsVisible = request.IsVisible,
                Roles = string.Join(",", request.Roles),
                Route = request.Route,
                MenuCategoryId = request.MenuCategoryId,
                Translations = request.Names.Select(x => new PageTranslation
                {
                    LanguageCode = x.Key,
                    Name = x.Value
                }).ToList()
            };

            var result = await _repository.CreateAsync(page);
            return MapToResponse(result);
        }

        public async Task<PageResponse> UpdateAsync(int id, CreatePageRequest request)
        {
            var existingPage = await _repository.GetByIdAsync(id);
            if (existingPage == null)
                throw new KeyNotFoundException($"Page with id {id} not found");

            existingPage.Icon = request.Icon;
            existingPage.Order = request.Order;
            existingPage.IsVisible = request.IsVisible;
            existingPage.Roles = string.Join(",", request.Roles);
            existingPage.Route = request.Route;
            existingPage.MenuCategoryId = request.MenuCategoryId;

            existingPage.Translations.Clear();
            foreach (var translation in request.Names)
            {
                existingPage.Translations.Add(new PageTranslation
                {
                    LanguageCode = translation.Key,
                    Name = translation.Value,
                    PageId = id
                });
            }

            var updatedPage = await _repository.UpdateAsync(id, existingPage);
            return MapToResponse(updatedPage);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _repository.DeleteAsync(id);
        }

        private PageResponse MapToResponse(Page page)
        {
            return new PageResponse
            {
                Id = page.Id,
                Names = page.Translations.ToDictionary(x => x.LanguageCode, x => x.Name),
                Icon = page.Icon,
                Order = page.Order,
                IsVisible = page.IsVisible,
                Roles = page.Roles.Split(',').ToList(),
                Route = page.Route,
                MenuCategoryId = page.MenuCategoryId
            };
        }
    }
} 