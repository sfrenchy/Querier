using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace $nameSpace$.ProcedureResultSets
{
    $procedureList: {procedure|
        public partial class $procedure.CSName$Result
        {
            $procedure.OutputSet: {column|
                [JsonPropertyName("$column.Name$")]
                [JsonProperty("$column.Name$", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include)]
                $column.ColumnAttribute$
                public $column.CSType$ $column.CSName$ { get; set; \}
            }$
        \}
    }$
\}