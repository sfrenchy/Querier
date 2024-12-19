using System.Collections.Generic;
using Querier.Api.Application.DTOs.Menu.Responses;

public class DynamicRowResponse
{
    public int Id { get; set; }
    public int Order { get; set; }
    public double? Height { get; set; }
    public List<DynamicCardResponse> Cards { get; set; }
} 