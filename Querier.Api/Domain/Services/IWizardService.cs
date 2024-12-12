using Querier.Api.Application.DTOs.Requests.Setup;
using System.Threading.Tasks;

namespace Querier.Api.Domain.Services
{
    public interface IWizardService
    {
        Task<(bool Success, string Error)> SetupAsync(SetupRequest request);
    }
}