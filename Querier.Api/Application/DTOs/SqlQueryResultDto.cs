using System.Collections.Generic;

namespace Querier.Api.Application.DTOs;

public class SqlQueryResultDto
{
    /// <summary>
    /// A boolean whose value will be true if the query succeeded and false if it didn't
    /// </summary>
    public bool QuerySuccessful { get; set; }

    /// <summary>
    /// The error message if the query didn't succeed
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// An entity definition
    /// </summary>
    public EntityDefinitionDto Entity { get; set; }

    /// <summary>
    /// A list to contain the data from an sql query
    /// </summary>
    public List<dynamic> Datas { get; set; }
}