using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Menu;

public interface IDynamicPageRepository
{
    Task<Page> GetByIdAsync(int id);
    Task<IEnumerable<Page>> GetAllAsync();
    Task<Page> CreateAsync(Page page);
    Task<Page> UpdateAsync(int id, Page page);
    Task<bool> DeleteAsync(int id);
} 
