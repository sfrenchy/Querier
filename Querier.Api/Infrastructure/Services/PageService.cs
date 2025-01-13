using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Domain.Entities.Menu;
using Querier.Api.Infrastructure.Data.Repositories;
using Page = Querier.Api.Domain.Entities.Menu.Page;

namespace Querier.Api.Infrastructure.Services
{
    public class PageService(IRoleRepository roleRepository, IPageRepository pageRepository, ILogger<PageService> logger) : IPageService
    {
        public async Task<PageDto> GetByIdAsync(int id)
        {
            logger.LogInformation("Getting page {PageId}", id);
            try
            {
                var page = await pageRepository.GetByIdAsync(id);
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
                var pages = await pageRepository.GetAllAsync();
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
                    Route = request.Route,
                    MenuId = request.MenuId,
                    Roles = string.Join(',', request.Roles.Select(r => r.Name)),
                    PageTranslations = request.Title.Select(x => new PageTranslation
                    {
                        LanguageCode = x.LanguageCode,
                        Name = x.Value
                    }).ToList()
                };
                
                Page result = await pageRepository.CreateAsync(page);

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
                var page = await pageRepository.GetByIdAsync(id);
                page.Icon = request.Icon;
                page.Order = request.Order;
                page.IsVisible = request.IsVisible;
                page.Route = request.Route;
                page.MenuId = request.MenuId;
                page.PageTranslations.Clear();
                page.PageTranslations = new List<PageTranslation>();
                page.Roles = string.Join(',', request.Roles.Select(r => r.Name));
                foreach (var translation in request.Title)
                    page.PageTranslations.Add(new PageTranslation() { LanguageCode = translation.LanguageCode, Name = translation.Value});
                
                var updatedPage = await pageRepository.UpdateAsync(id, page);
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
                var result = await pageRepository.DeleteAsync(id);
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

        public async Task<IEnumerable<PageDto>> GetAllByMenuIdAsync(int menuId)
        {
            logger.LogInformation("Getting pages for menu {MenuId}", menuId);
            try
            {
                var pages = await pageRepository.GetAllByMenuIdAsync(menuId);
                var result = pages.Select(PageDto.FromEntity).ToList();
                logger.LogInformation("Successfully retrieved {Count} pages for menu {MenuId}", result.Count, menuId);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving pages for menu {MenuId}", menuId);
                throw;
            }
        }
    }
} 