using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class AnalyzeQueryRequest
{
    [Required]
    public string Query { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
} 