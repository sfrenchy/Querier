using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace Querier.Api.Hubs
{
    [Authorize]
    public class ProgressHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Get the operation ID from the query string if provided
            var operationId = Context.GetHttpContext()?.Request.Query["operationId"].ToString();
            
            if (!string.IsNullOrEmpty(operationId))
            {
                // Add the client to the operation-specific group
                await Groups.AddToGroupAsync(Context.ConnectionId, operationId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var operationId = Context.GetHttpContext()?.Request.Query["operationId"].ToString();
            
            if (!string.IsNullOrEmpty(operationId))
            {
                // Remove the client from the operation-specific group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, operationId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Optional: Method to explicitly join an operation group
        public async Task JoinOperationGroup(string operationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, operationId);
        }

        // Optional: Method to leave an operation group
        public async Task LeaveOperationGroup(string operationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, operationId);
        }
    }
} 