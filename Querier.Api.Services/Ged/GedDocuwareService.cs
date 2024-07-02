using DocuWare.Platform.ServerClient;
using DocuWare.WebIntegration;
using Querier.Api.Models;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests.Ged;
using Querier.Api.Models.Responses;
using Querier.Api.Models.Responses.Ged;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Querier.Api.Models.Common;
using Querier.Api.Models.Enums.Ged;
using Querier.Api.Models.Ged;

namespace Querier.Api.Services.Ged
{
    public class GedDocuwareService : IQFileReadOnlyDeposit
    {
        //variable from interface;
        public QFileDeposit FileDepositInformations { get; set; }
        //

        private readonly ILogger<GedDocuwareService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;

        public GedDocuwareService(ILogger<GedDocuwareService> logger, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            FileDepositInformations = GetInformationFromDB();
        }

        private QFileDeposit GetInformationFromDB()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return apidbContext.QFileDeposit.FirstOrDefault(r => r.Type == TypeFileDepositEnum.Docuware);
            }
        } 

        public async Task<FillFileInformationResponse> FillFileInformations()
        {
            Uri uri = new Uri(FileDepositInformations.Host + "/DocuWare/Platform");

            //connection tp docuware
            ServiceConnection connection = ServiceConnection.Create(uri, FileDepositInformations.Login, FileDepositInformations.Password);

            //get all documents from FileDepositInformations.RootPath
            DocumentsQueryResult queryResult = await connection.GetFromDocumentsForDocumentsQueryResultAsync(FileDepositInformations.RootPath).ConfigureAwait(false); 

            List<Document> result = new List<Document>();
            result.AddRange(queryResult.Items);


            List<QFilesFromFileDeposit> response = new List<QFilesFromFileDeposit>();
            if(result.Count == 0)
            {
                return new FillFileInformationResponse() { success = false, numberFileAdded = 0, message = "no file finded" };
            }
            foreach (Document document in result)
            {
                QFilesFromFileDeposit element = new QFilesFromFileDeposit() { 
                    FileRef = document.Id.ToString(),
                    QFileDepositId = FileDepositInformations.Id,
                };
                element.SetConfiguration<ConfigurationDocuware>(new ConfigurationDocuware { Title = document.Title, StoredDate = document.CreatedAt, DateModification = document.LastModified });
                response.Add(element);
            }

            //save documents ref in QFilesFromFileDeposit
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //used to see il the file is already exist in table QFilesFromFileDeposit by compare the fileRef(path of the file) and the QFileDepositId
                List<QFilesFromFileDeposit> missingRecords = response.Where(x => !apidbContext.QFilesFromFileDeposit.Any(el => el.FileRef == x.FileRef && el.QFileDepositId == x.QFileDepositId )).ToList();
                
                if(missingRecords.Count > 0)
                {
                    apidbContext.QFilesFromFileDeposit.AddRange(missingRecords);
                    apidbContext.SaveChanges();
                    return new FillFileInformationResponse() { success = true, numberFileAdded = missingRecords.Count(), message = "" };
                }
                return new FillFileInformationResponse() { success = false, numberFileAdded = missingRecords.Count(), message = "all files already existed" };
                
            }
        }

        public async Task<List<GetInformationsResponse>> GetSpecificInformation(List<GetSpecificInformationRequest> variablesFilter)
        {
            Uri uri = new Uri(FileDepositInformations.Host + "/DocuWare/Platform");

            //connection a docuware
            ServiceConnection connection = ServiceConnection.Create(uri, FileDepositInformations.Login, FileDepositInformations.Password);

            //get all documents in result
            DocumentsQueryResult queryResult = await connection.GetFromDocumentsForDocumentsQueryResultAsync(FileDepositInformations.RootPath).ConfigureAwait(false);

            List<Document> result = new List<Document>();
            result.AddRange(queryResult.Items);

            if (result.Count == 0)
            {
                return new List<GetInformationsResponse>()
                    {
                        new GetInformationsResponse
                        {
                            FilePath = "",
                            IdQFilesFromFileDeposit = 0
                        }
                    };
            }

            //Get documents which have the variables 
            List<Document> documentsWithField = new List<Document>();
            List<Document> documentFiltered = new List<Document>();
            List<string> variableKeys = variablesFilter.Select(el => el.key).ToList();
            
            foreach (Document document in result)
            {
                //test if the document have a fiels corresponding with variablesFilter
                if (document.Fields.Count(f => variableKeys.Contains(f.FieldName)) > 0)
                {
                    documentsWithField.Add(document);
                }
            }

            //test if a document from documentsWithField have the same value in the field as variablesFilter
            foreach (Document document in documentsWithField)
            {
                foreach (var variable in variablesFilter)
                {
                    string key = variable.key;
                    string value = variable.value;

                    if (document.Fields.Any(f => f.FieldName == key))
                    {
                        if (document.Fields.First(f => f.FieldName == key).Item != null)
                        {
                            var fieldValue = document.Fields.First(f => f.FieldName == key).Item.ToString();
                            if (fieldValue == value)
                            {
                                documentFiltered.Add(document);
                            }   
                        }
                    }
                }
            }

            if (documentFiltered.Count() == 0)
            {
                return new List<GetInformationsResponse>()
                    {
                        new GetInformationsResponse
                        {
                            FilePath = "",
                            IdQFilesFromFileDeposit = 0
                        }
                    };
            }

            //with documents which corresponding with variablesFilter create the response list
            List<GetInformationsResponse> response = new List<GetInformationsResponse>();
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                foreach (Document document in documentFiltered)
                {
                    var doc = apidbContext.QFilesFromFileDeposit.FirstOrDefault(d => d.FileRef == document.Id.ToString());
                    if(doc != null)
                    {
                        response.Add(new GetInformationsResponse { FilePath = document.Title, IdQFilesFromFileDeposit = doc.Id });
                    }  
                }
            }
            return response;
        }

        public async Task<GeneralResponse> GetDocumentViewer(int IdTable)
        {
            int fileId;
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //get the file 
                QFilesFromFileDeposit fileReference = apidbContext.QFilesFromFileDeposit.FirstOrDefault(r => r.Id == IdTable && r.QFileDepositId == FileDepositInformations.Id);
                if (fileReference == null)
                {
                    return new GeneralResponse() { success = false, message = "file not find" };
                }
                // have a file id with type int 
                fileId = Int32.Parse(fileReference.FileRef);
            }

            //get the viewer of the file
            var serverUrl = FileDepositInformations.Host + "/DocuWare/Platform/WebClient";
            DWIntegrationInfo dwInfo = new DWIntegrationInfo(serverUrl, false);
            var integrationType = IntegrationType.Viewer;

            var dwParam = new DWIntegrationUrlParameters(integrationType)
            {
                FileCabinetGuid = Guid.Parse(FileDepositInformations.RootPath),
                DocId = fileId.ToString()
            };

            var dwUrl = new DWIntegrationUrl(dwInfo, dwParam);

            return new GeneralResponse() { success = true, message = dwUrl.Url };
        }

        //optional
        public List<FileCabinet> GetAllFileCabinet()
        {
            var uri = new Uri(FileDepositInformations.Host + "/DocuWare/Platform");
            ServiceConnection connection = ServiceConnection.Create(uri, FileDepositInformations.Login, FileDepositInformations.Password);
            var org = connection.Organizations[0];

            var fileCabinets = org.GetFileCabinetsFromFilecabinetsRelation().FileCabinet;
            return fileCabinets;
        }
    }

    
}
