using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Menu.Requests;
using Querier.Api.Application.DTOs.Menu.Responses;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface IDynamicCardService
    {
        Task<DynamicCardResponse> GetByIdAsync(int id);
        Task<IEnumerable<DynamicCardResponse>> GetByRowIdAsync(int rowId);
        Task<DynamicCardResponse> CreateAsync(int rowId, CreateDynamicCardRequest request);
        Task<DynamicCardResponse> UpdateAsync(int id, CreateDynamicCardRequest request);
        Task<bool> DeleteAsync(int id);
        Task<bool> ReorderAsync(int rowId, List<int> cardIds);
    }
} 