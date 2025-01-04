using System.Collections.Generic;

namespace Querier.Api.Infrastructure.Database.Models
{
    public class TemplateProperty
    {
        public string Name { get; set; }
        public string CSName { get; set; }
        public string CSType { get; set; }
        public bool IsKey { get; set; }
        public bool IsRequired { get; set; }
        public bool IsAutoGenerated { get; set; }
        public string SqlParameterType { get; set; }
        public string ColumnAttribute { get; set; }
    }

    public class TemplateEntityMetadata
    {
        public string Name { get; set; }
        public string PluralName { get; set; }
        public string KeyType { get; set; }
        public string KeyName { get; set; }
        public List<TemplateProperty> Properties { get; set; } = new List<TemplateProperty>();
    }

    public class TemplateModel
    {
        public string NameSpace { get; set; }
        public string ContextNameSpace { get; set; }
        public string ContextRoute { get; set; }
        public List<TemplateEntityMetadata> EntityList { get; set; } = new List<TemplateEntityMetadata>();
    }

    public class StoredProcedureMetadata
    {
        public string Name { get; set; }
        public string CSName { get; set; }
        public string CSReturnSignature { get; set; }
        public string CSParameterSignature { get; set; }
        public string InlineParameters { get; set; }
        public bool HasOutput { get; set; }
        public bool HasParameters { get; set; }
        public List<TemplateProperty> Parameters { get; set; } = new List<TemplateProperty>();
        public List<TemplateProperty> OutputSet { get; set; } = new List<TemplateProperty>();
        public List<string> SummableOutputColumns { get; set; } = new List<string>();
    }

    public class StoredProcedureTemplateModel
    {
        public string NameSpace { get; set; }
        public string ContextNameSpace { get; set; }
        public string ContextRoute { get; set; }
        public List<StoredProcedureMetadata> ProcedureList { get; set; } = new List<StoredProcedureMetadata>();
    }
} 