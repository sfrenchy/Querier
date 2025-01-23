using System.Threading.Tasks;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.DTOs.Requests.Setup;

namespace Querier.Api.Domain.Services
{
    public interface IWizardService
    {
        Task<(bool Success, string Error)> SetupAsync(SetupDto request);
    }
}