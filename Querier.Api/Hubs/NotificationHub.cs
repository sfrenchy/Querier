using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Notifications;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models.Common;

namespace Querier.Api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public static List<string> Users = new List<string>();
        private readonly INotification _notification;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        public NotificationHub(INotification notification, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _notification = notification;
            _contextFactory = contextFactory;
        }

        [HubMethodName("sendMessage")]
        public async Task SendMessageAsync(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message).ConfigureAwait(false);
        }

        public override Task OnConnectedAsync()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                Users.Add(Context.UserIdentifier);
                ApiUser user = apidbContext.Users.FirstOrDefault(u => u.Email == Context.UserIdentifier);
                if (user != null && apidbContext.QNotifications.Any(n => n.UserId == user.Id))
                {
                    foreach (QNotification notif in apidbContext.QNotifications.Where(n => n.UserId == user.Id))
                        _notification.NotifyUser(notif, false).GetAwaiter();
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

