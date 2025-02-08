using System.Threading.Tasks;
using Querier.Api.Domain.Models;

namespace Querier.Api.Domain.Services
{
    /// <summary>
    /// Service for sending real-time notifications to clients
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a progress update for a specific operation
        /// </summary>
        /// <param name="operationId">The unique identifier of the operation</param>
        /// <param name="progress">The progress event details</param>
        Task SendOperationProgressAsync(string operationId, ProgressEvent progress);
    }
} 