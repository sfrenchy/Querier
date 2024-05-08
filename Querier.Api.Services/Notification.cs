using Querier.Api.Models;
using Querier.Api.Models.Notifications;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Models.Common;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Services
{
    public interface INotification
    {
        Task NotifyUser(string userId, string content, bool persistNotification);
        Task NotifyUser(HANotification notif, bool persistNotification);
        Task MessageToUser(string from, string to, string content);
        Task MessageToAll(string from, string content);
        List<HANotification> GetPersistentNotifications();
    }

    public class Notification : INotification
    {
        private IHubContext<NotificationHub> _hubContext;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<Notification> _logger;
        public Notification(ILogger<Notification> logger, IHubContext<NotificationHub> hubContext, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _hubContext = hubContext;
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task MessageToUser(string from, string to, string content)
        {
            if (NotificationHub.Users.Contains(to))
                await _hubContext.Clients.User(to).SendAsync("ChatMessageToUser", from, to, content).ConfigureAwait(false);
        }

        public async Task MessageToAll(string from, string content)
        {
            await _hubContext.Clients.All.SendAsync("ChatMessageToAll", from, content).ConfigureAwait(false);
        }

        public async Task NotifyUser(string userEmail, string jsonContent, bool persistNotification)
        {
            _logger.LogInformation($"NotifyUser: userEmail={userEmail}, jsonContect={jsonContent}, persistNotification={persistNotification}");
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                string notificationId = System.Guid.NewGuid().ToString();
                if (persistNotification)
                {
                    HANotification un = new HANotification();
                    un.Id = notificationId;
                    un.Date = System.DateTime.Now;
                    un.UserId = apidbContext.Users.First(u => u.Email == userEmail).Id;
                    un.JsonContent = jsonContent;

                    apidbContext.Add(un);
                    await apidbContext.SaveChangesAsync();

                    if (NotificationHub.Users.Contains(userEmail)) {
                        _logger.LogInformation("Notifying persistent notification via signalR");
                        await _hubContext.Clients.User(userEmail).SendAsync("ToastPersistentNotification", un.Id, un.Date, un.JsonContent).ConfigureAwait(false);
                    }
                }
                else
                {
                    if (NotificationHub.Users.Contains(userEmail)) {
                        _logger.LogInformation("Notifying non-persistent via signalR");
                        await _hubContext.Clients.User(userEmail).SendAsync("ToastNotification", notificationId, jsonContent).ConfigureAwait(false);
                    }
                }
                
            }
        }

        public async Task NotifyUser(HANotification notif, bool persistNotification)
        {
            if (NotificationHub.Users.Contains(notif.User.Email))
                await _hubContext.Clients.User(notif.User.Email).SendAsync("ToastPersistentNotification", notif.Id, notif.Date, notif.JsonContent).ConfigureAwait(false);
        }

        public List<HANotification> GetPersistentNotifications()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return apidbContext.HANotifications.ToList();
            }
        }
    }
}
