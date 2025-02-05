using System.Threading.Tasks;

namespace Querier.Api.Infrastructure.Services
{
    public interface IEncryptionService
    {
        Task<string> EncryptAsync(string plainText);
        Task<string> DecryptAsync(string encryptedText);
    }
} 