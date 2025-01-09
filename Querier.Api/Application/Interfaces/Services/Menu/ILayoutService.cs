using System.Threading.Tasks;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Application.Interfaces.Services.Menu
{
    public interface ILayoutService
    {
        Task<LayoutDto> GetLayoutAsync(int pageId);
        Task<LayoutDto> UpdateLayoutAsync(int pageId, LayoutDto layout);
        Task<bool> DeleteLayoutAsync(int pageId);
    }
} 