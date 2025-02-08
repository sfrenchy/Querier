using Microsoft.AspNetCore.SignalR;
using Querier.Api.Hubs;
using Querier.Api.Domain.Models;
using System.Threading.Tasks;

namespace Querier.Api.Domain.Services
{
    /// <summary>
    /// Implementation of the notification service using SignalR
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<QuerierHub> _hubContext;

        public NotificationService(IHubContext<QuerierHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendOperationProgressAsync(string operationId, ProgressEvent progress)
        {
            // Send progress update to all clients subscribed to this operation
            await _hubContext.Clients
                .Group($"operation_{operationId}")
                .SendAsync("OperationProgress", progress);
        }
    }
} 