namespace Querier.Api.Domain.Common.Models;

public class OrderByParameterDto
{
    public string Column { get; set; }
    public bool IsDescending { get; set; }
}