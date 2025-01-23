using System.Collections.Generic;

namespace Querier.Api.Domain.Common.Models;

public class DataRequestParametersWtihSQLParametersDto : DataRequestParametersDto
{
    public Dictionary<string, object> Parameters { get; set; }
}