using Querier.Api.Domain.Entities.Menu;

namespace Querier.Api.Application.DTOs.Menu.Requests
{
    public class CreateDynamicRowRequest
    {
        public int Order { get; set; }
        public MainAxisAlignment Alignment { get; set; }
        public CrossAxisAlignment CrossAlignment { get; set; }
        public double Spacing { get; set; }
    }
} 