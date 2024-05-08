using Querier.Api.Models;
using Querier.Api.Models.Notifications;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Querier.Api.Models.Common;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly INotification _notification;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        public NotificationController(IDbContextFactory<ApiDbContext> contextFactory, INotification notification, ILogger<NotificationController> logger)
        {
            _notification = notification;
            _logger = logger;
            _contextFactory = contextFactory;
        }

        [HttpGet("AcknowledgeNotification")]
        public async Task AcknowledgeNotification([FromQuery] string id)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QNotification n = apidbContext.HANotifications.Find(id);
                if (n != null)
                {
                    apidbContext.Remove(n);
                    await apidbContext.SaveChangesAsync();
                }
            }
        }
    }
}
