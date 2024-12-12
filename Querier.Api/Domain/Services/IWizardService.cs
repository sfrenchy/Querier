using Querier.Api.Models.Requests;
using System.Threading.Tasks;

namespace Querier.Api.Services
{
    public interface IWizardService
    {
        Task<(bool Success, string Error)> SetupAsync(SetupRequest request);
    }
} 