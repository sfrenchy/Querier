using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace $nameSpace$.ProcedureParameters
{
    $procedureList: {procedure|
        public partial class $procedure.CSName$Params
        {
            $procedure.Parameters: {parameter|
                [JsonPropertyName("$parameter.CSName$")]
                [JsonProperty("$parameter.CSName$", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include)]
                public $parameter.CSType$ $parameter.CSName$ { get; set; \}
            }$
        \}
    }$
\}