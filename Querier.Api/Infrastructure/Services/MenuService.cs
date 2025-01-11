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
    public class MenuService(IMenuRepository repository, ILogger<MenuService> logger) : IMenuService
    {
        public async Task<List<MenuDto>> GetAllAsync()
        {
            logger.LogInformation("Getting all menu categories");
            try
            {
                var categories = await repository.GetAllAsync();
                var result = categories.Select(MenuDto.FromEntity).ToList();
                logger.LogInformation("Successfully retrieved {Count} menu categories", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all menu categories");
                throw;
            }
        }

        public async Task<MenuDto> GetByIdAsync(int id)
        {
            logger.LogInformation("Getting menu category {CategoryId}", id);
            try
            {
                var category = await repository.GetByIdAsync(id);
                if (category == null)
                {
                    logger.LogWarning("Menu category {CategoryId} not found", id);
                    return null;
                }

                var result = MenuDto.FromEntity(category);
                logger.LogInformation("Successfully retrieved menu category {CategoryId}", id);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving menu category {CategoryId}", id);
                throw;
            }
        }

        public async Task<MenuDto> CreateAsync(MenuCreateDto request)
        {
            if (request == null)
            {
                logger.LogError("Attempted to create a menu category with null request");
                throw new ArgumentNullException(nameof(request));
            }

            logger.LogInformation("Creating new menu category");
            try
            {
                var category = new Menu
                {
                    Icon = request.Icon,
                    Order = request.Order,
                    IsVisible = request.IsVisible,
                    Roles = string.Join(",", request.Roles ?? new List<string>()),
                    Route = request.Route,
                    Translations = request.Names.Select(x => new MenuTranslation
                    {
                        LanguageCode = x.Key,
                        Name = x.Value
                    }).ToList()
                };

                var result = await repository.CreateAsync(category);
                var response = MenuDto.FromEntity(result);
                logger.LogInformation("Successfully created menu category {CategoryId}", response.Id);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating menu category");
                throw;
            }
        }

        public async Task<MenuDto> UpdateAsync(int id, MenuCreateDto request)
        {
            if (request == null)
            {
                logger.LogError("Attempted to update menu category {CategoryId} with null request", id);
                throw new ArgumentNullException(nameof(request));
            }

            logger.LogInformation("Updating menu category {CategoryId}", id);
            try
            {
                var category = await repository.GetByIdAsync(id);
                if (category == null)
                {
                    logger.LogWarning("Menu category {CategoryId} not found for update", id);
                    return null;
                }

                category.Icon = request.Icon;
                category.Order = request.Order;
                category.IsVisible = request.IsVisible;
                category.Roles = string.Join(",", request.Roles ?? new List<string>());
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
                var response = MenuDto.FromEntity(result);
                logger.LogInformation("Successfully updated menu category {CategoryId}", id);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating menu category {CategoryId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            logger.LogInformation("Deleting menu category {CategoryId}", id);
            try
            {
                var result = await repository.DeleteAsync(id);
                if (result)
                {
                    logger.LogInformation("Successfully deleted menu category {CategoryId}", id);
                }
                else
                {
                    logger.LogWarning("Menu category {CategoryId} not found for deletion", id);
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting menu category {CategoryId}", id);
                throw;
            }
        }
    }
} 