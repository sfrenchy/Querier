using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Querier.Api.Models.Notifications.MQMessages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Querier.Api.Services.MQServices
{
	public class ToastMessageReceiverService : BackgroundService
    {
		private readonly IConfiguration _configuration;
		private readonly IServiceProvider _serviceProvider;
		private readonly IHubContext<NotificationHub> _hubContext;
        private readonly INotification _notification;
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<ToastMessageReceiverService> _logger;

        public ToastMessageReceiverService(ILogger<ToastMessageReceiverService> logger, IConfiguration configuration, IServiceProvider serviceProvider, IHubContext<NotificationHub> hubContext, INotification notification)
        {
			_configuration = configuration;
			_serviceProvider = serviceProvider;
			_hubContext = hubContext;
            _notification = notification;
            _logger = logger;
            _factory = new ConnectionFactory() { HostName = configuration["RabbitMQ:Host"], Port = Convert.ToInt32(configuration["RabbitMQ:Port"]) };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "ToastMessageQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
            _notification = notification;
            // _notification = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<INotification>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _channel.Dispose();
                _connection.Dispose();
                return Task.CompletedTask;
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Task.Run(() =>
                {
                    if(message != null && message != "")
                    {
                        _logger.LogInformation("Sending signalR ToastMessage");
                        ToastMessage toast = JsonConvert.DeserializeObject<ToastMessage>(message);
                        string toastContent = toast.ToJSONString();

                        _notification.NotifyUser(toast.Recipient, toastContent, toast.Persistent);
                    }
                });
            };

            _channel.BasicConsume(queue: "ToastMessageQueue", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

    }
}

