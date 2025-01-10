using System.Threading.Tasks;

namespace Querier.Api.Application.Interfaces.Services
{
    public interface IAspnetIdentityConfigurationService
    {
        Task ConfigureIdentityOptions();
        Task ConfigureTokenProviderOptions();
    }
} 