using System.Collections.Generic;
using Querier.Api.Domain.Entities;

public class CreateUpdateSQLQueryDTO
{
    public SQLQuery Query { get; set; }
    public Dictionary<string, object> SampleParameters { get; set; }
}