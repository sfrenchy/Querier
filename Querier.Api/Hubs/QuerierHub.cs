using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Querier.Api.Models;

namespace Querier.Api.Hubs
{
    /// <summary>
    /// Central hub for all real-time notifications in the application
    /// </summary>
    [Authorize]
    public class QuerierHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribes the client to operation progress updates
        /// </summary>
        public async Task SubscribeToOperation(string operationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"operation_{operationId}");
        }

        /// <summary>
        /// Unsubscribes the client from operation progress updates
        /// </summary>
        public async Task UnsubscribeFromOperation(string operationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"operation_{operationId}");
        }

        public async Task SendOperationProgress(OperationProgress progress)
        {
            await Clients.All.SendAsync("operationProgress", progress);
        }
    }
} 