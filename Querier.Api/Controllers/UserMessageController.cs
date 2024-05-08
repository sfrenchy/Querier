using Querier.Api.Models;
using Querier.Api.Models.Messages;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("[controller]")]
    [ApiController]
    public class UserMessageController : ControllerBase
    {
        private readonly ILogger<UserMessageController> _logger;
        private readonly INotification _notification;
        public UserMessageController(INotification notification, ILogger<UserMessageController> logger)
        {
            _notification = notification;
            _logger = logger;
        }

        [HttpPost("SendToAll")]
        public async Task SendToAllAsync([FromBody] UserMessage message)
        {
            await _notification.MessageToAll(message.From, message.MessageContent);
        }
    }
}
