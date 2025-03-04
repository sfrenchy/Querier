using System.Collections.Generic;
using System.Linq;

namespace Querier.Api.Infrastructure.Database.Templates;

public class StoredProcedureMetadata
{
    public string Schema { get; set; }
    public string Name { get; set; }
    public string CSName { get; set; }
    public string CSReturnSignature
    {
        get
        {
            if (!HasOutput)
                return "Task";
            return $"Task<List<{CSName}Dto>>";
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
    
    public string InlineParameters
    {
        get
        {
            string result = Parameters.Aggregate("", (current, parameter) => current + $"{parameter.Name},");

            if (Parameters.Count > 0)
                result = result.Substring(0, result.Length - 1);

            return result;
        }
    }
    public bool HasOutput => OutputSet != null && OutputSet.Count > 0;
    public bool HasParameters => Parameters != null && Parameters.Count > 0;
    public List<TemplateProperty> Parameters { get; set; } = new();
    public List<TemplateProperty> OutputSet { get; set; } = new();
}