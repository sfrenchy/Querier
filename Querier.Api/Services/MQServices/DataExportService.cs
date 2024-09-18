using System;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;
using Querier.Api.Models.Requests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Querier.Api.Services.MQServices
{
	public class DataExportReceiverService : BackgroundService
    {
        private readonly IConfiguration _configuration;
		private readonly IServiceProvider _serviceProvider;
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IEntityCRUDService _entityCRUDService;
        private readonly IToastMessageEmitterService _toastMessageEmitterService;
        private readonly IQUploadService _uploadService;
        private readonly ILogger<DataExportReceiverService> _logger;
        private readonly IDynamicContextList _dynamicContextList;
        private readonly IExportGeneratorService _exportGeneratorService;
        
        public DataExportReceiverService(ILogger<DataExportReceiverService> logger, 
                                         IDynamicContextList dynamicContextList, 
                                         IQUploadService uploadService, 
                                         IToastMessageEmitterService toastMessageEmitterService, 
                                         IConfiguration configuration, 
                                         IServiceProvider serviceProvider, 
                                         IEntityCRUDService entityCRUDService,
                                         IExportGeneratorService exportGeneratorService)
        {
            _logger = logger;
            _uploadService = uploadService;
            _toastMessageEmitterService = toastMessageEmitterService;
            _serviceProvider = serviceProvider;
            _entityCRUDService = entityCRUDService;
            _dynamicContextList = dynamicContextList;
            _exportGeneratorService = exportGeneratorService;
            _factory = new ConnectionFactory() { HostName = configuration["RabbitMQ:Host"], Port = Convert.ToInt32(configuration["RabbitMQ:Port"]) };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "DataExportQueue",
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);
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
                ExportRequest message = MQMessage.FromBytes<ExportRequest>(ea.Body.ToArray());
                Task.Run(async () => { 
                    try
                    {
                        HAUploadUrl downloadURL = await _exportGeneratorService.GenerateExport(message);
                        ToastMessage exportAvailableMessage = new ToastMessage();
                        exportAvailableMessage.TitleCode = "lbl-export-available-title";
                        exportAvailableMessage.Recipient = message.RequestUserEmail;
                        exportAvailableMessage.ContentCode = "lbl-export-available-content";
                        exportAvailableMessage.ContentDownloadURL = downloadURL.Url + downloadURL.FileId;
                        exportAvailableMessage.ContentDownloadsFilename = $"{message.Configuration.exportName}.{message.FileType}";
                        exportAvailableMessage.Closable = true;
                        exportAvailableMessage.Persistent = true;
                        exportAvailableMessage.Type = ToastType.Success;
                        _logger.LogInformation("Publishing export notification");
                        _toastMessageEmitterService.PublishToast(exportAvailableMessage);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error while generating export");
                        ToastMessage exportAvailableMessage = new ToastMessage();
                        exportAvailableMessage.TitleCode = "lbl-export-error-title";
                        exportAvailableMessage.Recipient = message.RequestUserEmail;
                        exportAvailableMessage.ContentCode = "lbl-export-error-content";
                        exportAvailableMessage.Closable = true;
                        exportAvailableMessage.Persistent = false;
                        exportAvailableMessage.Type = ToastType.Danger;
                        _logger.LogInformation("Publishing export error notification");
                        _toastMessageEmitterService.PublishToast(exportAvailableMessage);
                    }
                });
            };

            _channel.BasicConsume(queue: "DataExportQueue", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }
    }
}