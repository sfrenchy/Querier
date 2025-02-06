using System.Collections.Generic;

namespace Querier.Api.Domain.Common.Models;

public class DataRequestParametersWithSQLParametersDto : DataRequestParametersDto
{
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}