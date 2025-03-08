using System.Collections.Generic;

namespace Querier.Api.Infrastructure.Database.Templates;

public class TemplateModel
{
    public string RootNamespace { get; set; }
    public string ContextNamespace { get; set; }
    public string ContextName { get; set; }
    public string ModelNamespace { get; set; }
    public string ContextRoute { get; set; }
    public List<TemplateEntityMetadata> EntityList { get; set; } = new();
    public List<StoredProcedureMetadata> ProcedureList { get; set; } = new();
}