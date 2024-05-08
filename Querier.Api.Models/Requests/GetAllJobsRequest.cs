using Querier.Api.Models.Datatable;

namespace Querier.Api.Models.Requests;

public class GetAllJobsRequest
{
    public ServerSideRequest datatableRequest { get; set; }
    public string ClientTimeZone { get; set; }
}