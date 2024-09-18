using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Querier.Api.Models.Requests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Querier.Api.Models;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;
using Microsoft.Extensions.Logging;
using Querier.Api.Tools;

namespace Querier.Api.Services.MQServices
{
	public class DataImportReceiverService : BackgroundService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
		private readonly IServiceProvider _serviceProvider;
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IEntityCRUDService _entityCRUDService;
        private readonly IToastMessageEmitterService _toastMessageEmitterService;
        private readonly IEntityCRUDService _crudService;
        private readonly  ILogger<DataImportReceiverService> _logger;
        public DataImportReceiverService(ILogger<DataImportReceiverService> logger, IEntityCRUDService crudService, IToastMessageEmitterService toastMessageEmitterService, IWebHostEnvironment webHostEnvironment, IConfiguration configuration, IServiceProvider serviceProvider, IEntityCRUDService entityCRUDService)
        {
            _logger = logger;
            _crudService = crudService;
            _toastMessageEmitterService = toastMessageEmitterService;
            _webHostEnvironment = webHostEnvironment;
            _serviceProvider = serviceProvider;
            _entityCRUDService = entityCRUDService;
            _factory = new ConnectionFactory() { HostName = configuration["RabbitMQ:Host"], Port = Convert.ToInt32(configuration["RabbitMQ:Port"]) };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "DataImportQueue",
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
                ImportEntitiesFromCSVRequest message = MQMessage.FromBytes<ImportEntitiesFromCSVRequest>(ea.Body.ToArray());
                Task.Run(() => { ImportEntitiesFromCSV(message); });
            };

            _channel.BasicConsume(queue: "DataImportQueue", autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        private void ImportEntitiesFromCSV(ImportEntitiesFromCSVRequest importParameters)
        {
            try
            {
                var datas = _entityCRUDService.Read(importParameters.contextType, importParameters.entityType, new List<DataFilter>(), out Type reqType);
                
                List<Type> contextTypes = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(assembly => assembly.GetTypes())
                        .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == importParameters.contextType).ToList();
                DbContext context =  ServiceActivator.GetScope().ServiceProvider.GetService(contextTypes.First()) as DbContext;           

                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Quote = '"',
                    Delimiter = ";"
                };

                var keyName = context.Model.FindEntityType(reqType).FindPrimaryKey().Properties.Select(x => x.Name).Single();
                
                using (var reader = new StreamReader(importParameters.filePath))
                using (var csv = new CsvReader(reader, csvConfig))
                {
                    csv.Read();
                    csv.ReadHeader();
                    csv.ValidateHeader(reqType);
                    
                    while (csv.Read())
                    {
                        var record = csv.GetRecord(reqType);

                        var oIdentifierValue = record.GetType().GetProperty(importParameters.identifierColumn).GetValue(record);
                        if (datas.Any(d => d.GetType().GetProperty(importParameters.identifierColumn).GetValue(d).ToString() == oIdentifierValue.ToString()))
                        {
                            if (importParameters.allowUpdate)
                            {
                                var dbData = context.Find(reqType, reqType.GetProperty(keyName).GetValue(record));
                                if (dbData != null)
                                    context.Entry(dbData).CurrentValues.SetValues(record);
                            }
                        }
                        else
                        {
                            var dbData = context.Add(Activator.CreateInstance(reqType));
                            Dictionary<string, object> Values = new Dictionary<string, object>();
                            string toto = dbData.Metadata.FindPrimaryKey().GetName();
                            foreach (PropertyInfo pi in reqType.GetProperties())
                            {
                                
                                if (pi.Name != "Id")
                                    Values.Add(pi.Name, pi.GetValue(record));
                            }

                            dbData.CurrentValues.SetValues(Values);
                        }
                    }
                    context.SaveChanges();  
                }
                _toastMessageEmitterService.PublishToast(new ToastMessage() {
                    Closable = true,
                    Persistent = false,
                    Recipient = importParameters.requestUserEmail,
                    TitleCode = "lbl-import-success",
                    ContentCode = "lbl-import-success-content",
                    Type = ToastType.Success
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                _toastMessageEmitterService.PublishToast(new ToastMessage() {
                    Closable = true,
                    Persistent = false,
                    Recipient = importParameters.requestUserEmail,
                    TitleCode = "lbl-import-failed",
                    ContentCode = "lbl-import-failed-content",
                    Type = ToastType.Danger
                });
            }
        }
    }
}