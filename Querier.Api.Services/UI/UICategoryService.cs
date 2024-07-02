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
        Task<QPageCategory> GetCategoryAsync(int categoryId);
        Task<List<QPageCategory>> GetCategoriesAsync();
        Task<List<QPageCategory>> AddCategoryAsync(AddCategoryRequest request);
        Task<List<QPageCategory>> UpdateCategoryAsync(UpdateCategoryRequest request);
        Task<List<QPageCategory>> DeleteCategoryAsync(QPageCategory category);
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

        public async Task<QPageCategory> GetCategoryAsync(int categoryId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.QPageCategories.FindAsync(categoryId);
            }
        }

        public async Task<List<QPageCategory>> GetCategoriesAsync()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.QPageCategories.ToListAsync();
            }
        }

        public async Task<List<QPageCategory>> AddCategoryAsync(AddCategoryRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                apidbContext.QPageCategories.Add(new QPageCategory()
                {
                    Label = request.Label,
                    Description = request.Description,
                    Icon = request.Icon
                });
                await apidbContext.SaveChangesAsync();

                return await apidbContext.QPageCategories.ToListAsync();
            }
        }

        public async Task<List<QPageCategory>> UpdateCategoryAsync(UpdateCategoryRequest request)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPageCategory category = await apidbContext.QPageCategories.FindAsync(request.Id);
                category.Label = request.Label;
                category.Description = request.Description;
                category.Icon = request.Icon;

                await apidbContext.SaveChangesAsync();

                return await apidbContext.QPageCategories.ToListAsync();
            }
        }

        public async Task<List<QPageCategory>> DeleteCategoryAsync(QPageCategory category)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                apidbContext.QPageCategories.Remove(category);

                await apidbContext.SaveChangesAsync();

                return await apidbContext.QPageCategories.ToListAsync();
            }
        }
    }
}
