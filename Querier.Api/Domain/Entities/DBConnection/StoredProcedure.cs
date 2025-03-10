using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Querier.Api.Domain.Entities.DBConnection;

namespace Querier.Api.Domain.Entities.QDBConnection
{
    public class StoredProcedure
    {
        public string Schema { get; set; }
        public string Name { get; set; }

        public string CSName
        {
            get
            {
                return SQLStringTools.NormalizeProcedureNameCSString(Name);
            }
        }

        [JsonProperty("SummableOutputColumns")]
        public List<string> SummableOutputColumns
        {
            get
            {
                List<string> result = new List<string>();
                List<string> summableTypes = new List<string>() {
                    "long",
                    "long?",
                    "decimal",
                    "decimal?",
                    "double",
                    "double?",
                    "int",
                    "int?",
                    "single",
                    "single?",
                    "short",
                    "short?"
                };

                foreach (ProcedureOutput output in OutputSet.OrderBy(o => o.Order))
                {
                    if (summableTypes.Contains(output.CSType.ToLower()))
                    {
                        result.Add(output.Name.ToUpper());
                    }
                }

                return result;
            }
        }

        public List<ProcedureParameter> Parameters { get; set; }
        public List<ProcedureOutput> OutputSet { get; set; }
        public bool HasParameters { get { return Parameters != null && Parameters.Count > 0; } }
        public bool HasOutput { get { return OutputSet != null && OutputSet.Count > 0; } }

        public string InlineParameters
        {
            get
            {
                string result = "";

                foreach (var parameter in Parameters)
                    result += $"{parameter.Name},";

                if (Parameters.Count > 0)
                    result = result.Substring(0, result.Length - 1);

                return result;
            }
        }

        public string CSParameterSignature
        {
            get
            {
                string result = "OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default";
                if (HasParameters)
                {
                    result = $"{CSName}InputDto inputDto, {result}";
                }
                return result;
            }
        }

        public string CSReturnSignature
        {
            get
            {
                if (!HasOutput)
                    return "Task";
                return $"Task<List<{CSName}Dto>>";
            }
        }
    }
}