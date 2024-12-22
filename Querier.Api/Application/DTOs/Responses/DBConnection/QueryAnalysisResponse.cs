using System.Collections.Generic;

public class QueryAnalysisResponse
{
    public List<string> Tables { get; set; } = new();
    public List<string> Views { get; set; } = new();
    public List<string> StoredProcedures { get; set; } = new();
    public List<string> UserFunctions { get; set; } = new();
} 