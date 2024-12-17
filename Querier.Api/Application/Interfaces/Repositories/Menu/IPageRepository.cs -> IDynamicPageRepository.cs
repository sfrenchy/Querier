using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDynamicPageRepository
{
    Task<DynamicPage> GetByIdAsync(int id);
    Task<IEnumerable<DynamicPage>> GetAllAsync();
    Task<DynamicPage> CreateAsync(DynamicPage page);
    Task<DynamicPage> UpdateAsync(int id, DynamicPage page);
    Task<bool> DeleteAsync(int id);
} 