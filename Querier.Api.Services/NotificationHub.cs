using Querier.Api.Models;
using Querier.Api.Models.Notifications;
using Querier.Api.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models.Common;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Services
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public static List<string> Users = new List<string>();
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly INotification _notification;
        private readonly ILogger<NotificationHub> _logger;
        public NotificationHub(ILogger<NotificationHub> logger, INotification notification, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
            _notification = notification;
            _logger = logger;
        }

        [HubMethodName("sendMessage")]
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message).ConfigureAwait(false);
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"New signalR client connexion: {Context.UserIdentifier}");
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                Users.Add(Context.UserIdentifier);
                ApiUser user = apidbContext.Users.FirstOrDefault(u => u.Email == Context.UserIdentifier);
                if (user != null && apidbContext.HANotifications.Any(n => n.UserId == user.Id))
                {
                    foreach (HANotification notif in apidbContext.HANotifications.Where(n => n.UserId == user.Id))
                        _notification.NotifyUser(notif, true).GetAwaiter();
                }

                return base.OnConnectedAsync();
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Users.Remove(Context.UserIdentifier);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
