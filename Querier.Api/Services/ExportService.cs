using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Querier.Api.Services
{
    public interface IExportService
    {
        void AskExport(ExportRequest exportParameters);
    }

    public class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;
        private readonly IConfiguration _configuration;

        public ExportService(IConfiguration configuration, ILogger<ExportService> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void AskExport(ExportRequest exportParameters)
        {
            var factory = new ConnectionFactory() { HostName = _configuration["RabbitMQ:Host"], Port = Convert.ToInt32(_configuration["RabbitMQ:Port"]) };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "DataExportQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                channel.BasicPublish(exchange: "",
                                                 routingKey: "DataExportQueue",
                                                 basicProperties: null,
                                                 body: ((IMQMessage)exportParameters).GetBytes());
            }
        }
    }
}