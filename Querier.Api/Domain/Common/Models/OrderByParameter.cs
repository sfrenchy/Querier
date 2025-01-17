namespace Querier.Api.Domain.Common.Models;

public class OrderByParameter
{
    public string Column { get; set; }
    public bool IsDescending { get; set; }
}