using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicCardService
    {
        Task<CardDto> GetByIdAsync(int id);
        Task<IEnumerable<CardDto>> GetByRowIdAsync(int rowId);
        Task<CardDto> CreateAsync(int rowId, CardDto request);
        Task<CardDto> UpdateAsync(int id, CardDto request);
        Task<bool> DeleteAsync(int id);
        Task<bool> ReorderAsync(int rowId, List<int> cardIds);
    }
} 