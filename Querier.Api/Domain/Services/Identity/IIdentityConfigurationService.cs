using System.Threading.Tasks;

namespace Querier.Api.Domain.Services.Identity
{
    public interface IIdentityConfigurationService
    {
        Task ConfigureIdentityOptions();
        Task ConfigureTokenProviderOptions();
    }
} 