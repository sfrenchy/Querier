using System.Threading.Tasks;
using Querier.Api.Application.DTOs.Menu.Responses;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface ILayoutService
    {
        Task<LayoutResponse> GetLayoutAsync(int pageId);
        Task<LayoutResponse> UpdateLayoutAsync(int pageId, LayoutResponse layout);
        Task<bool> DeleteLayoutAsync(int pageId);
    }
} 