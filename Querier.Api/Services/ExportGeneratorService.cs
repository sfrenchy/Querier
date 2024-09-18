using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Data;
using System.Linq;
using System.Net.Http;
using ClosedXML.Excel;
using Querier.Api.Models.Common;
using CsvHelper.Configuration;
using System.Text;
using System.Threading.Tasks;
using Querier.Api.Models;
using Querier.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Querier.Api.Tools;

namespace Querier.Api.Services
{
    public interface IExportGeneratorService
    {
        public Task<HAUploadUrl> GenerateExport(ExportRequest exportParameters);
    }

    public class ExportGeneratorService : IExportGeneratorService
    {
        private readonly ILogger<ExportGeneratorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEntityCRUDService _entityCRUDService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDynamicContextList _dynamicContextList;
        private readonly IQUploadService _uploadService;
        private readonly HttpClient _httpClient;
        public ExportGeneratorService(IConfiguration configuration, 
                                      ILogger<ExportGeneratorService> logger, 
                                      IEntityCRUDService entityCRUDService,
                                      IServiceProvider serviceProvider,
                                      IDynamicContextList dynamicContextList,
                                      IQUploadService uploadService)
        {
            _logger = logger;
            _configuration = configuration;
            _entityCRUDService = entityCRUDService;
            _serviceProvider = serviceProvider;
            _dynamicContextList = dynamicContextList;
            _uploadService = uploadService;
            _httpClient = new HttpClient();
        }

        public async Task<HAUploadUrl> GenerateExport(ExportRequest exportParameters)
        {
            _logger.LogInformation("Generating export");
            string filename = "";
            string extension = "";
            DataTable datas = await GetDatasFromDataTable(exportParameters);
            DataTable exportData = new DataTable("exportData");


            if (exportParameters.DataSource.Type == ExportSourceType.dynamicContextProcedureService)
            {
                // Iterate over the key-value pairs using foreach
                foreach (KeyValuePair<string, string> kvp in exportParameters.ExportedColumns)
                {
                    string keyProcessed = kvp.Key.Replace("_", "");

                    DataColumn col = datas.Columns[keyProcessed];
                    if (col != null)
                    {
                        // Add the column with the same name and data type to the exportData DataTable
                        exportData.Columns.Add(kvp.Value, col.DataType);
                    }
                }

                // Iterate through each row in datas
                foreach (DataRow sourceRow in datas.Rows)
                {
                    // Create a new row in exportData
                    DataRow destRow = exportData.NewRow();

                    // Populate the new row with data from the corresponding columns in datas
                    foreach (KeyValuePair<string, string> kvp in exportParameters.ExportedColumns)
                    {
                        string keyProcessed = kvp.Key.Replace("_", "");
                        DataColumn col = datas.Columns[keyProcessed];
                        if (col != null)
                        {
                            // Copy the data from sourceRow to destRow for each corresponding column
                            destRow[kvp.Value] = sourceRow[col];
                        }
                    }
                    // Add the populated row to exportData
                    exportData.Rows.Add(destRow);
                }
            }
            
            HAUploadUrl downloadURL = new HAUploadUrl();

            switch (exportParameters.FileType)
            {
                case ExportType.csv:
                    downloadURL = await GenerateCSV(exportData, exportParameters);
                    filename = exportParameters.Configuration.exportName;
                    extension = "csv";
                    break;
                case ExportType.xlsx:
                    downloadURL = await GenerateXLSX(exportData, exportParameters);
                    filename = exportParameters.Configuration.exportName;
                    extension = "xlsx";
                    break;
                default:
                    throw new NotImplementedException($"ExportType {exportParameters.FileType} not yet implemented");
            }
            
            return downloadURL;
        }

        private async Task<DataTable> GetDatasFromDataTable(ExportRequest exportParameters)
        {
            DataTable datas = new DataTable();
            switch (exportParameters.DataSource.Type)
            {
                case ExportSourceType.sql:
                    ExportDataSourceSQLQuery sqlQueryExpression = ((JObject)exportParameters.DataSource.Expression).ToObject<ExportDataSourceSQLQuery>();
                    datas = _entityCRUDService.GetDatatableFromSql(sqlQueryExpression.DynamicContextName, sqlQueryExpression.SQLQuery, exportParameters.Filters);
                    break;
                case ExportSourceType.entity:
                    ExportDataSourceEntity entityExpression = ((JObject)exportParameters.DataSource.Expression).ToObject<ExportDataSourceEntity>();
                    List<object> objects = _entityCRUDService.Read(
                        entityExpression.DynamicContextName,
                        entityExpression.EntityName,
                        exportParameters.Filters,
                        out Type entityType).ToList();
                    datas = (DataTable)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(objects), typeof(DataTable));
                    break;
                case ExportSourceType.dynamicContextProcedureService:
                    ExportDataSourceDynamicContextProcedure dynamicContextProcedureExpression = exportParameters.DataSource.Expression is JObject ? 
                                                                                                    ((JObject)exportParameters.DataSource.Expression).ToObject<ExportDataSourceDynamicContextProcedure>() :
                                                                                                    (ExportDataSourceDynamicContextProcedure)exportParameters.DataSource.Expression;
                    string dynamicContextName = dynamicContextProcedureExpression.DynamicContextName;
                    if (_dynamicContextList.DynamicContexts.TryGetValue(dynamicContextName, out IDynamicContextProceduresServicesResolver? dynamicContextProcedureResolver))
                    {
                        var service = _serviceProvider.GetService(dynamicContextProcedureResolver.ProcedureNameService[dynamicContextProcedureExpression.ServiceName]);
                        if (service is IDynamicContextProcedureWithParamsAndResult)
                        {
                            datas = (await ((IDynamicContextProcedureWithParamsAndResult)service).DatasAsync(dynamicContextProcedureExpression.Parameters)).ToDataTable();
                        }
                        else
                        {
                            throw new Exception($"The service for '{dynamicContextProcedureExpression.ServiceName}' doesn't implement IDynamicContextProcedureWithParamsAndResult");
                        }
                    }
                    else
                    {
                        throw new Exception($"Unable to find a dynamic context named '{dynamicContextName}'");
                    }
                    break;
                default:
                    throw new NotImplementedException(
                        $"Export type {exportParameters.DataSource.Type} not implemented yet");
            }

            // We want all datas for now
            exportParameters.DatatableRequest.start = 0;
            exportParameters.DatatableRequest.length = -1;

            if (exportParameters.UseFilters)
            {
                var filteredDatas = datas.AsEnumerable().DatatableFilter(exportParameters.DatatableRequest, out int? count);
                return filteredDatas.CopyToDataTable();
            }
            else
            {
                return datas;
            }
        }

        private async Task<HAUploadUrl> GenerateXLSX(DataTable datas, ExportRequest exportParameters)
        {
            string xlsxFilePath = Path.GetTempFileName() + ".xlsx";
            using (var workbook = new XLWorkbook())
            {
                workbook.AddWorksheet(datas, $"{exportParameters.Configuration.exportName}");
                workbook.SaveAs(xlsxFilePath);
            }

            HAUploadDefinitionFromApi uploadDef = new HAUploadDefinitionFromApi()
            {
                Definition = new SimpleUploadDefinition()
                {
                    FileName = $"{exportParameters.Configuration.exportName}",
                    MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    MaxSize = 1000000000, // 1Gb around
                    Nature = exportParameters.Nature
                },
                UploadStream = File.Open(xlsxFilePath, FileMode.Open)
            };

            HAUploadUrl result = new HAUploadUrl()
            {
                Url = "api/HAUpload/GetFile/",
                FileId = await _uploadService.UploadFileFromApiAsync(uploadDef)
            };

            return result;
        }

        private async Task<HAUploadUrl> GenerateCSV(DataTable datas, ExportRequest exportParameters)
        {
            var csvConf = new CsvConfiguration(CultureInfo.InvariantCulture);
            csvConf.Delimiter = exportParameters.Configuration.Delimiter;
            csvConf.ShouldQuote = (args) => exportParameters.Configuration.QuoteAllFields;
            // csvConf.CultureInfo = CultureInfo.InvariantCulture;

            string csvFilePath = Path.GetTempFileName();
            using (var writer = new StreamWriter(csvFilePath, Encoding.UTF8, new FileStreamOptions() { Access = FileAccess.Write, Mode = FileMode.OpenOrCreate }))
            {
                using (var csv = new CsvWriter(writer, csvConf))
                {
                    foreach (DataColumn column in datas.Columns)
                    {
                        csv.WriteField(column.ColumnName);
                    }

                    csv.NextRecord();

                    foreach (DataRow row in datas.Rows)
                    {
                        for (int i = 0; i < datas.Columns.Count; i++)
                        {
                            csv.WriteField(row[i]);
                        }

                        csv.NextRecord();
                    }
                }
            }

            HAUploadDefinitionFromApi uploadDef = new HAUploadDefinitionFromApi()
            {
                Definition = new SimpleUploadDefinition()
                {
                    FileName = $"{exportParameters.Configuration.exportName}",
                    MimeType = "text/csv",
                    MaxSize = 1000000000, // 1Gb around
                    Nature = exportParameters.Nature
                },
                UploadStream = File.Open(csvFilePath, FileMode.Open)
            };

            HAUploadUrl result = new HAUploadUrl()
            {
                Url = "api/HAUpload/GetFile/",
                FileId = await _uploadService.UploadFileFromApiAsync(uploadDef)
            };
            return result;
        }
    }
}