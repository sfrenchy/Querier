using Querier.Api.Application.DTOs.Requests.Setup;
using System.Threading.Tasks;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Domain.Services
{
    public interface IWizardService
    {
        Task<(bool Success, string Error)> SetupAsync(SetupDto request);
    }
}