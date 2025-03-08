namespace Querier.Api.Infrastructure.Database.Templates;

public class TemplateForeignKey
{
    public string Name { get; set; }
    public string NamePlural { get; set; }
    public string ReferencedEntityPlural { get; set; }
    public string ReferencedEntitySingular { get; set; }
    public string ReferencedColumn { get; set; }
    public bool IsCollection { get; set; }
}