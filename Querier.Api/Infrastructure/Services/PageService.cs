using System;
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
    public class PageService(IPageRepository repository, ILogger<PageService> logger) : IPageService
    {
        public async Task<PageDto> GetByIdAsync(int id)
        {
            logger.LogInformation("Getting page {PageId}", id);
            try
            {
                var page = await repository.GetByIdAsync(id);
                if (page == null)
                {
                    logger.LogWarning("Page {PageId} not found", id);
                    return null;
                }

                var result = PageDto.FromEntity(page);
                logger.LogInformation("Successfully retrieved page {PageId}", id);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving page {PageId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<PageDto>> GetAllAsync()
        {
            logger.LogInformation("Getting all pages");
            try
            {
                var pages = await repository.GetAllAsync();
                var result = pages.Select(PageDto.FromEntity).ToList();
                logger.LogInformation("Successfully retrieved {Count} pages", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all pages");
                throw;
            }
        }

        public async Task<PageDto> CreateAsync(PageCreateDto request)
        {
            if (request == null)
            {
                logger.LogError("Attempted to create a page with null request");
                throw new ArgumentNullException(nameof(request));
            }

            logger.LogInformation("Creating new page");
            try
            {
                var page = new Page
                {
                    Icon = request.Icon,
                    Order = request.Order,
                    IsVisible = request.IsVisible,
                    Roles = string.Join(",", request.Roles ?? Enumerable.Empty<string>()),
                    Route = request.Route,
                    MenuId = request.DynamicMenuCategoryId,
                    PageTranslations = request.Names.Select(x => new PageTranslation
                    {
                        LanguageCode = x.Key,
                        Name = x.Value
                    }).ToList()
                };

                var result = await repository.CreateAsync(page);
                var response = PageDto.FromEntity(result);
                logger.LogInformation("Successfully created page {PageId}", response.Id);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating page");
                throw;
            }
        }

        public async Task<PageDto> UpdateAsync(int id, PageUpdateDto request)
        {
            if (request == null)
            {
                logger.LogError("Attempted to update page {PageId} with null request", id);
                throw new ArgumentNullException(nameof(request));
            }

            logger.LogInformation("Updating page {PageId}", id);
            try
            {
                var page = new Page
                {
                    Icon = request.Icon,
                    Order = request.Order,
                    IsVisible = request.IsVisible,
                    Roles = request.Roles == null ? string.Empty : string.Join(",", request.Roles),
                    Route = request.Route,
                    MenuId = request.DynamicMenuCategoryId,
                    PageTranslations = request.Translations.Select(t => new PageTranslation
                    {
                        LanguageCode = t.LanguageCode,
                        Name = t.Name
                    }).ToList()
                };

                var updatedPage = await repository.UpdateAsync(id, page);
                if (updatedPage == null)
                {
                    logger.LogWarning("Page {PageId} not found for update", id);
                    return null;
                }

                var response = PageDto.FromEntity(updatedPage);
                logger.LogInformation("Successfully updated page {PageId}", id);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating page {PageId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            logger.LogInformation("Deleting page {PageId}", id);
            try
            {
                var result = await repository.DeleteAsync(id);
                if (result)
                {
                    logger.LogInformation("Successfully deleted page {PageId}", id);
                }
                else
                {
                    logger.LogWarning("Page {PageId} not found for deletion", id);
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting page {PageId}", id);
                throw;
            }
        }

    }
} 