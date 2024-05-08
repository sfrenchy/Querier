using System.Collections.Generic;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Notifications.MQMessages;

namespace Querier.Api.Models.Requests
{
    public class ExportRequest : MQMessage
    {
        public ServerSideRequest DatatableRequest { get; set; }
        public string RequestUserEmail { get; set; }
        public ExportType FileType { get; set; }
        public ExportDataSource DataSource { get; set; }
        public dynamic Configuration { get; set; }
        public List<DataFilter> Filters { get; set; } = new List<DataFilter>();
        public Dictionary<string, string> ExportedColumns { get; set; } = new Dictionary<string, string>();
        public bool UseFilters { get; set; } = true;
        public HAUploadNatureEnum Nature { get; set; }

    }
    
    public class ExportDataSourceBase
    {
        public string DynamicContextName { get; set; }
    }
    public class ExportDataSourceDynamicContextProcedure : ExportDataSourceBase
    {
        public string ServiceName { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class ExportDataSourceSQLQuery : ExportDataSourceBase
    {
        public string SQLQuery { get; set; }
    }

    public class ExportDataSourceEntity : ExportDataSourceBase
    {
        public string EntityName { get; set; }
    }
    
    public class ExportDataSource
    {
        public ExportSourceType Type { get; set; }
        public object Expression { get; set; }
    }

    public class CsvExportConfiguration
    {
        public bool QuoteAllFields { get; set; }
        public string Delimiter { get; set;}
    }
}