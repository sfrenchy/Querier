using System;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Querier.Api.Models;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Services.MQServices
{

    public class ToastMessageEmitterService : IToastMessageEmitterService
    {
        private readonly IConfiguration _configuration;
		private readonly IServiceProvider _serviceProvider;
		private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<ToastMessageEmitterService> _logger;
        public ToastMessageEmitterService(ILogger<ToastMessageEmitterService> logger, IConfiguration configuration, IServiceProvider serviceProvider, IHubContext<NotificationHub> hubContext)
        {
            _configuration = configuration;
			_serviceProvider = serviceProvider;
			_hubContext = hubContext;
            _logger = logger;
            _factory = new ConnectionFactory() { HostName = configuration["RabbitMQ:Host"], Port = Convert.ToInt32(configuration["RabbitMQ:Port"]) };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "ToastMessageQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
        }

        public void PublishToast(ToastMessage message)
        {
            _logger.LogInformation("Add ToastMessage to Queue");
            _channel.BasicPublish(exchange: "",
                                                 routingKey: "ToastMessageQueue",
                                                 basicProperties: null,
                                                 body: message.GetBytes());
        }
    }
}