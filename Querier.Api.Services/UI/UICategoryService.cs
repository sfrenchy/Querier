using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Services.UI
{

    public interface IUICategoryService
    {
        Task<HAPageCategory> GetCategoryAsync(int categoryId);
        Task<List<HAPageCategory>> GetCategoriesAsync();
        Task<List<HAPageCategory>> AddCategoryAsync(AddCategoryRequest request);
        Task<List<HAPageCategory>> UpdateCategoryAsync(UpdateCategoryRequest request);
        Task<List<HAPageCategory>> DeleteCategoryAsync(HAPageCategory category);
    }
    public class UICategoryService : IUICategoryService
    {
        private readonly ILogger<UICategoryService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;

        public UICategoryService(ILogger<UICategoryService> logger, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task<HAPageCategory> GetCategoryAsync(int categoryId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.HAPageCategories.FindAsync(categoryId);
            }
        }

        public async Task<List<HAPageCategory>> GetCategoriesAsync()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.HAPageCategories.ToListAsync();
            }
        }

        public async Task<List<HAPageCategory>> AddCategoryAsync(AddCategoryRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                apidbContext.HAPageCategories.Add(new HAPageCategory()
                {
                    Label = request.Label,
                    Description = request.Description,
                    Icon = request.Icon
                });
                await apidbContext.SaveChangesAsync();

                return await apidbContext.HAPageCategories.ToListAsync();
            }
        }

        public async Task<List<HAPageCategory>> UpdateCategoryAsync(UpdateCategoryRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPageCategory category = await apidbContext.HAPageCategories.FindAsync(request.Id);
                category.Label = request.Label;
                category.Description = request.Description;
                category.Icon = request.Icon;

                await apidbContext.SaveChangesAsync();

                return await apidbContext.HAPageCategories.ToListAsync();
            }
        }

        public async Task<List<HAPageCategory>> DeleteCategoryAsync(HAPageCategory category)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                apidbContext.HAPageCategories.Remove(category);

                await apidbContext.SaveChangesAsync();

                return await apidbContext.HAPageCategories.ToListAsync();
            }
        }
    }
}
