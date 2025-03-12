using System.Collections.Generic;

namespace Querier.Api.Infrastructure.Database.Templates;

public class TemplateEntityMetadata
{
    public string Name { get; set; }
    public string PluralName { get; set; }
    public List<string> KeyNames { get; set; } = new List<string>();
    public List<string> KeyTypes { get; set; } = new List<string>();
    public bool IsViewEntity { get; set; }

    public bool IsTableEntity => !IsViewEntity;

    public List<TemplateProperty> Properties { get; set; } = new();
    public List<TemplateForeignKey> ForeignKeys { get; set; } = new();
}