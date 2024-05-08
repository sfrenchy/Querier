using $nameSpace$.ProcedureParameters;
using HerdiaApp.Api.Models.Datatable;
namespace $nameSpace$.ProcedureReportRequests
{
    $procedureList: {procedure|
        public partial class $procedure.CSName$ReportRequest
        {
            public ServerSideRequest DatatableParams { get;set; \}
            public $procedure.CSName$Params ProcedureParams { get; set; \}
        \}
    }$
\}