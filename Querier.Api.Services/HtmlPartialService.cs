using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Responses;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Querier.Api.Models.Interfaces;

namespace Querier.Api.Services
{
    public interface IHtmlPartialService
    {
        Task<HtmlPartialResponse> GetHtmlPart(int cardId, string writtenLanguage);
        Task<dynamic> CreateFilePartialAsync(string Content, string writtenLanguage, int cardId);
    }
    public class HtmlPartialService : IHtmlPartialService
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IHAUploadService _uploadService;
        public HtmlPartialService(IHAUploadService uploadService, IDbContextFactory<ApiDbContext> contextFactory)
        {
            _uploadService = uploadService;

            _contextFactory = contextFactory;
        }
        public async Task<HtmlPartialResponse> GetHtmlPart(int cardId, string writtenLanguage)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAHtmlPartialRef fileInfoFromDB = apidbContext.HAHtmlPartialRefs.FirstOrDefault(r => r.HAPageCard.Id == cardId && r.Language == writtenLanguage);
                if (fileInfoFromDB == null)
                {
                    //throw new System.NullReferenceException();
                }
                else
                {
                    int uploadId = fileInfoFromDB.HAUploadDefinitionId;
                    bool zipped = fileInfoFromDB.Zipped;
                    string language = fileInfoFromDB.Language;
                    byte[] byteArrayFileResult;
                    if (zipped) //file zipped 
                    {
                        Stream fileStream = await _uploadService.GetUploadStream(uploadId);
                        byte[] byteArrayFile;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            fileStream.CopyTo(ms);
                            byteArrayFile = ms.ToArray();
                        }

                        //unZippe
                        byte[] unzippedArray;
                        using (var zippedStream = new MemoryStream(byteArrayFile))
                        {
                            using (var archive = new ZipArchive(zippedStream))
                            {
                                var entry = archive.Entries.FirstOrDefault();

                                if (entry != null)
                                {
                                    using (var unzippedEntryStream = entry.Open())
                                    {
                                        using (var ms = new MemoryStream())
                                        {
                                            unzippedEntryStream.CopyTo(ms);
                                            unzippedArray = ms.ToArray();
                                        }
                                    }
                                }
                                else
                                {
                                    unzippedArray = null;
                                }
                            }
                        }
                        byteArrayFileResult = unzippedArray;
                    }
                    else //file not zipped 
                    {
                        Stream fileStream = await _uploadService.GetUploadStream(uploadId);
                        using (MemoryStream ms = new MemoryStream())
                        {
                            fileStream.CopyTo(ms);
                            byteArrayFileResult = ms.ToArray();
                        }
                    }
                    HtmlPartialResponse response = new HtmlPartialResponse();
                    response.Content = System.Text.Encoding.UTF8.GetString(byteArrayFileResult);
                    response.Language = language;
                    return response;
                }
            }
            return null;
        }

        public async Task<dynamic> CreateFilePartialAsync(string Content, string writtenLanguage, int cardId)
        {
            dynamic response;
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                //get the file from the request :
                byte[] fileContentBytes = Encoding.Default.GetBytes(Content);

                //here we create zip version of the file for see which one is smaller :
                //zip compression
                byte[] fileContentBytesZipped = null;

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, false))
                    {

                        var entry = archive.CreateEntry("entry.bin");

                        using (var entryStream = entry.Open())
                        using (var streamWriter = new BinaryWriter(entryStream))
                        {
                            streamWriter.Write(fileContentBytes, 0, fileContentBytes.Length);
                        }
                    }
                    fileContentBytesZipped = memoryStream.ToArray();
                }

                //compare
                byte[] fileContentResult;
                bool zipped;
                if (fileContentBytesZipped.Length == null || fileContentBytes.Length == null)
                {
                    //throw new System.NullReferenceException();
                    response = new { success = false, uploadId = 0000 };
                    return response;
                }
                else
                {
                    byte[][] ArraysBytes = { fileContentBytesZipped, fileContentBytes };
                    var fileContentMin = ArraysBytes.Min(t => t.Length);
                    if (fileContentMin == fileContentBytesZipped.Length)
                    {
                        fileContentResult = fileContentBytesZipped;
                        zipped = true;
                    }
                    else
                    {
                        fileContentResult = fileContentBytes;
                        zipped = false;
                    }
                    //upload the file
                    string FilePath = Path.GetTempFileName();
                    HAUploadDefinitionFromApi uploadDef = new HAUploadDefinitionFromApi()
                    {
                        Definition = new SimpleUploadDefinition()
                        {
                            FileName = "html_partial.html",
                        },
                        UploadStream = new MemoryStream(fileContentResult)
                    };
                    int upload_Id = await _uploadService.UploadFileFromApiAsync(uploadDef);

                    //verify in db if there is already html partial in this card 
                    HAHtmlPartialRef fileInfoFromDB = apidbContext.HAHtmlPartialRefs.FirstOrDefault(r => r.HAPageCard.Id == cardId && r.Language == writtenLanguage);
                    if (fileInfoFromDB == null)
                    {
                        //not find  
                        //add Html Partial Refs informations to the db :
                        await apidbContext.HAHtmlPartialRefs.AddAsync(new HAHtmlPartialRef()
                        {
                            HAUploadDefinitionId = upload_Id,
                            Language = writtenLanguage,
                            Zipped = zipped,
                            HAPageCardId = cardId
                        });

                        await apidbContext.SaveChangesAsync();
                    }
                    else
                    {
                        //find
                        //remove the previous informations 
                        apidbContext.HAHtmlPartialRefs.Remove(fileInfoFromDB);
                        await apidbContext.SaveChangesAsync();
                        //add new information to the db :
                        await apidbContext.HAHtmlPartialRefs.AddAsync(new HAHtmlPartialRef()
                        {
                            HAUploadDefinitionId = upload_Id,
                            Language = writtenLanguage,
                            Zipped = zipped,
                            HAPageCardId = cardId
                        });
                        await apidbContext.SaveChangesAsync();
                    }
                    response = new { success = true, uploadId = upload_Id };
                    return response;

                }
            }
        }

    }
}
