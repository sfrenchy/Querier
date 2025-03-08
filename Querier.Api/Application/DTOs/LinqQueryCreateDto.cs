using System.Collections.Generic;

namespace Querier.Api.Application.DTOs;

public class LinqQueryCreateDto
{
    public LinqQueryDto Query { get; set; }
    public Dictionary<string, object> SampleParameters { get; set; }
}