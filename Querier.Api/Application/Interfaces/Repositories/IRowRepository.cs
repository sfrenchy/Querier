using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.Interfaces.Repositories
{
    public interface IRowRepository
    {
        Task<Row> GetByIdAsync(int id);
        Task<IEnumerable<Row>> GetByPageIdAsync(int pageId);
        Task<Row> CreateAsync(Row row);
        Task<Row> UpdateAsync(Row row);
        Task<Row> UpdateAsync(int id, Row row);
        Task<bool> DeleteAsync(int id);
        Task<int> GetMaxOrderInPageAsync(int pageId);
    }
} 