using System.Globalization;
using System.IO;
using CsvHelper;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Querier.Api.Services
{
    public interface IImportService
    {
        void AskImportEntitiesFromCSV(ImportEntitiesFromCSVRequest importParameters);
    }

    public class ImportService : IImportService
    {
        private readonly ILogger<ImportService> _logger;
        private readonly IConfiguration _configuration;

        public ImportService(IConfiguration configuration, ILogger<ImportService> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        void IImportService.AskImportEntitiesFromCSV(ImportEntitiesFromCSVRequest importParameters)
        {
            var factory = new ConnectionFactory() { HostName = _configuration["RabbitMQ:Host"], Port = Convert.ToInt32(_configuration["RabbitMQ:Port"])};
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "DataImportQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                channel.BasicPublish(exchange: "",
                                                 routingKey: "DataImportQueue",
                                                 basicProperties: null,
                                                 body: ((IMQMessage)importParameters).GetBytes());
            }
            
        }
    }
}