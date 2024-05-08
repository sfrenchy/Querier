using System.Collections.Generic;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Enums.Ged;

namespace Querier.Api.Models.Requests.Ged
{
    public class GetDatatableSpecificInfosRequest
    {
        public ServerSideRequest requestDatatable { get; set; }
        public List<GetSpecificInformationRequest> variablesFilter { get; set; }
        public TypeFileDepositEnum type { get; set; }
    }
}
