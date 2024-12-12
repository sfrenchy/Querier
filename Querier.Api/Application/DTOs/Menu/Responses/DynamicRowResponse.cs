using System.Collections.Generic;
using Querier.Api.Application.DTOs.Menu.Responses;

public class DynamicRowResponse
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Alignment { get; set; }
    public string CrossAlignment { get; set; }
    public double Spacing { get; set; }
    public List<DynamicCardResponse> Cards { get; set; }
} 