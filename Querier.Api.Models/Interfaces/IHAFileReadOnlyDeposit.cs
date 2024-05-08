using Querier.Api.Models.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Models.Ged;
using Querier.Api.Models.Requests.Ged;
using Querier.Api.Models.Responses;
using Querier.Api.Models.Responses.Ged;

namespace Querier.Api.Models.Interfaces
{
    public interface IHAFileReadOnlyDeposit
    {
        public HAFileDeposit FileDepositInformations { get; set; }

        //this method is used to fill the table HAFilesFromFileDeposit with files from the used file deposit 
        Task<FillFileInformationResponse> FillFileInformations();
        //this method is used to get a specific information from a file by using the filter column in the table HAFileDeposit
        Task<List<GetInformationsResponse>> GetSpecificInformation(List<GetSpecificInformationRequest> variablesFilter);
        //this method is used to get the viewer url of the file deposit, if the file deposit does not have a viewer (we know by looking in the capabilities column in the table HAFileDeposit)
        //the file will be send to be dowloaded 
        Task<GeneralResponse> GetDocumentViewer(int fileId);
    }
}
