using $nameSpace$.ProcedureParameters;
namespace $nameSpace$.ProcedureReportRequests
{
    $procedureList: {procedure|
        public partial class $procedure.CSName$ReportRequest
        {
            public $procedure.CSName$Params ProcedureParams { get; set; \}
        \}
    }$
\}