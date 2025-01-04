using System.Collections.Generic;

namespace Querier.Api.Infrastructure.Database.Models
{
    public class StoredProcedure
    {
        public string Name { get; set; }
        public string CSName { get; set; }
        public string CSReturnSignature { get; set; }
        public string CSParameterSignature { get; set; }
        public string InlineParameters { get; set; }
        public bool HasOutput { get; set; }
        public bool HasParameters { get; set; }
        public List<StoredProcedureParameter> Parameters { get; set; }
        public List<StoredProcedureColumn> OutputSet { get; set; }
        public List<string> SummableOutputColumns { get; set; }
    }

    public class StoredProcedureParameter
    {
        public string Name { get; set; }
        public string CSName { get; set; }
        public string CSType { get; set; }
        public string SqlParameterType { get; set; }
    }

    public class StoredProcedureColumn
    {
        public string Name { get; set; }
        public string CSName { get; set; }
        public string CSType { get; set; }
    }
} 