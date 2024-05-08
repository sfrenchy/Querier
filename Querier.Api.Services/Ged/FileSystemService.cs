using Antlr4.StringTemplate;
using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests.Ged;
using Querier.Api.Models.Responses;
using Querier.Api.Models.Responses.Ged;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using Querier.Api.Models.Enums.Ged;
using Querier.Api.Models.Ged;

namespace Querier.Api.Services.Ged
{
    public class FileSystemService : IHAFileReadOnlyDeposit
    {
        //variable from interface;
        public HAFileDeposit FileDepositInformations { get; set; }
        //

        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<FileSystemService> _logger;
        private readonly IHAUploadService _uploadService;

        public FileSystemService(ILogger<FileSystemService> logger, IDbContextFactory<ApiDbContext> contextFactory, IHAUploadService uploadService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            FileDepositInformations = GetInformationFromDB();
            _uploadService = uploadService;
        }
        private HAFileDeposit GetInformationFromDB()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return apidbContext.HAFileDeposit.FirstOrDefault(r => r.Type == TypeFileDepositEnum.FileSystem);
            }
        }
        public async Task<FillFileInformationResponse> FillFileInformations()
        {

            //if(FileDepositInformations.RootPath.Last() != Path.DirectorySeparatorChar)
            //{
            //    FileDepositInformations.RootPath += Path.DirectorySeparatorChar;
            //}

            //get all file paths also in child directories
            string[] filePaths = Directory.GetFiles(FileDepositInformations.RootPath, "*", SearchOption.AllDirectories);

            if(filePaths.Length == 0) 
            {
                return new FillFileInformationResponse() { success = false, numberFileAdded = 0, message = "no files found in the file system" };
            }

            //get informations for all files found
            List<HAFilesFromFileDeposit> response = new List<HAFilesFromFileDeposit>();
            foreach (string filePath in filePaths) 
            {
                FileInfo fileInfo = new FileInfo(filePath);

                HAFilesFromFileDeposit element = new HAFilesFromFileDeposit
                {
                    FileRef = fileInfo.FullName,
                    HAFileDepositId = FileDepositInformations.Id,
                };
                element.SetConfiguration<ConfigurationFileSystem>(new ConfigurationFileSystem
                {
                    DateCreation = fileInfo.CreationTime,
                    DateModification = fileInfo.LastWriteTime,
                    LastAcces = fileInfo.LastAccessTime
                });
                response.Add(element);
            }

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //used to see il the file is already exist in table HAFilesFromFileDeposit by compare the fileRef(path of the file) and the HAFileDepositId
                List<HAFilesFromFileDeposit> missingRecords = response.Where(new_el => 
                    !apidbContext.HAFilesFromFileDeposit.Any(el_exist => 
                        el_exist.FileRef == new_el.FileRef && 
                        el_exist.HAFileDepositId == new_el.HAFileDepositId
                 )).ToList();

                //save the missing file in the table 
                if (missingRecords.Count > 0)
                {
                    await apidbContext.HAFilesFromFileDeposit.AddRangeAsync(missingRecords);
                    apidbContext.SaveChanges();
                    return new FillFileInformationResponse() { success = true, numberFileAdded = missingRecords.Count(), message = "" };
                }
                return new FillFileInformationResponse() { success = false, numberFileAdded = missingRecords.Count(), message = "all files already existed" };
            }
        }

        public async Task<List<GetInformationsResponse>> GetSpecificInformation(List<GetSpecificInformationRequest> variablesFilter)
        {
            //if (FileDepositInformations.RootPath.Last() != Path.DirectorySeparatorChar)
            //{
            //    FileDepositInformations.RootPath += Path.DirectorySeparatorChar;
            //}

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //get all files from the file deposit 
                List<HAFilesFromFileDeposit> filesFromSpecificFileDeposit = apidbContext.HAFilesFromFileDeposit.Where(r => r.HAFileDepositId == FileDepositInformations.Id).ToList();
                if (filesFromSpecificFileDeposit == null)
                {
                    return new List<GetInformationsResponse>()
                        {
                            new GetInformationsResponse
                            {
                                FilePath = "",
                                IdHAFilesFromFileDeposit = 0
                            }
                        };
                }
                //get all expressions from the column filter 
                var expressionsFilter = FileDepositInformations.ConfigurationFilter;

                var expressionUsable = new List<string>();

                //get expressions which have the same parameters as variablesFilter 
                foreach (var expression in expressionsFilter)
                {
                    List<string> variablesInExpression = new List<string>();

                    // find variables in the expression (separator($))
                    MatchCollection matches = Regex.Matches(expression, @"\$([^$]+)\$");
                    bool allVariablesExist = false;
                    
                    foreach (Match match in matches)
                    {
                        string variable = match.Groups[1].Value;
                        variablesInExpression.Add(variable);
                    }
                    //test whether the variables in the expression are the same as those passed to variablefilter
                    allVariablesExist = variablesInExpression.All(variable => variablesFilter.Exists(v => v.key == variable));
                                    
                    if (allVariablesExist)
                    {
                        expressionUsable.Add(expression);
                    }
                    else
                    {
                        continue;
                    }
                }

                //fill expressions with variables using StringTemplate
                List<string> expressionsWithVariableAdded = new List<string>();
                foreach (var expression in expressionUsable)
                {
                    var template = new Template(expression, '$', '$');
                    for (var i = 0; i < variablesFilter.Count(); i++)
                    {
                        //name of the variable
                        var keyDictionary = variablesFilter[i].key;
                        if (expression.Contains(keyDictionary))
                        {
                            template.Add(keyDictionary, variablesFilter[i].value);
                        }
                    }
                    expressionsWithVariableAdded.Add(template.Render());
                }

                //then find the files that match the filled expression using fileref 
                List<GetInformationsResponse> result = new List<GetInformationsResponse>();
                foreach (HAFilesFromFileDeposit file in filesFromSpecificFileDeposit)
                {
                    string filePath = file.FileRef;
                    
                    foreach (var expression in expressionsWithVariableAdded)
                    {
                        if (filePath.Contains(expression))
                        {
                            result.Add(new GetInformationsResponse()
                            {
                                FilePath = filePath,
                                IdHAFilesFromFileDeposit = file.Id,
                            });
                        }
                    }
                }
                return result;
            }
        }

        public async Task<GeneralResponse> GetDocumentViewer(int IdTable)
        {
            //if (FileDepositInformations.RootPath.Last() != Path.DirectorySeparatorChar)
            //{
            //    FileDepositInformations.RootPath += Path.DirectorySeparatorChar;
            //}

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //get the file from HAFilesFromFileDeposit
                HAFilesFromFileDeposit fileReference = apidbContext.HAFilesFromFileDeposit.FirstOrDefault(r => r.Id == IdTable && r.HAFileDepositId == FileDepositInformations.Id);
                if (fileReference == null)
                {
                    return new GeneralResponse () { success = false, message = "file not find" };
                }

                //we use HAUploadDefinitions to get a download url, as we don't yet have a viewer. 

                //test if we have already a uploadDefinitionRef
                HAUploadDefinition uploadDefinitionRef = apidbContext.HAUploadDefinitions.FirstOrDefault(r => r.FileName == fileReference.FileRef);
                int UploadId;
                ConfigurationFileSystem fileConfig = fileReference.GetConfiguration<ConfigurationFileSystem>();
                if (uploadDefinitionRef == null)
                {
                    //upload file in uplod definition
                    byte[] fileByteArray = File.ReadAllBytes(fileReference.FileRef);

                    HAUploadDefinitionFromApi requestParam = new HAUploadDefinitionFromApi()
                    {
                        Definition = new SimpleUploadDefinition()
                        {
                            FileName = fileReference.FileRef,
                            Nature = HAUploadNatureEnum.FileDeposit,
                            MimeType = "text/plain"
                        },
                        UploadStream = new MemoryStream(fileByteArray)
                    };
                    UploadId = await _uploadService.UploadFileFromApiAsync(requestParam);
                    //
                }
                else
                {
                    //if yes,  compare the modification dates; if they are different, delete and re-upload the file.
                    DateTime LastModificationActual = new FileInfo(fileReference.FileRef).LastWriteTime;
                    DateTime LastModificationOrigin = fileConfig.DateModification;
                    
                    if (LastModificationActual == LastModificationOrigin)
                    {
                        UploadId = uploadDefinitionRef.Id;
                    }
                    else
                    {
                        await _uploadService.DeleteUploadAsync(uploadDefinitionRef.Id);

                        //upload file in uplod definition
                        byte[] fileByteArray = File.ReadAllBytes(fileReference.FileRef);

                        HAUploadDefinitionFromApi requestParam = new HAUploadDefinitionFromApi()
                        {
                            Definition = new SimpleUploadDefinition()
                            {
                                FileName = fileReference.FileRef,
                                Nature = HAUploadNatureEnum.FileDeposit,
                                MimeType = "text/plain",
                            },
                            UploadStream = new MemoryStream(fileByteArray)
                        };
                        UploadId = await _uploadService.UploadFileFromApiAsync(requestParam);
                        //

                    }
                }

                string downloadURL = $"api/HAUpload/GetFile/{UploadId}";

                return new GeneralResponse() { success = true, message = downloadURL };
            }
        }
    }
}
