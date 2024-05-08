﻿using System.IO.Compression;
using System.Transactions;
using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Requests;
using Querier.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Querier.Api.Services
{


    public class HAUploadService : IHAUploadService
    {
        private readonly ILogger<HAUploadService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public HAUploadService(ILogger<HAUploadService> logger, IDbContextFactory<ApiDbContext> contextFactory, IWebHostEnvironment hostEnvironment, IConfiguration configuration)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _environment = hostEnvironment;
            _configuration = configuration;
        }

        public async Task<string> UploadFileFromVMAsync(HAUploadDefinitionVM upload)
        {
            HAUploadDefinition newObject = new HAUploadDefinition();

            var forbiddenMimeTypeList = _configuration.GetSection("ApplicationSettings:UploadSettings:UploadForbiddenTypes").Get<List<string>>();
            var maxSize = _configuration.GetSection("ApplicationSettings:UploadSettings:UploadMaxSize").Get<string>();

            string mimeType = "";

            //Use byte array sequences to determine the correct MIME type of a given file. 
            using (var ms = new MemoryStream())
            {
                upload.File.CopyTo(ms);
                var fileBytes = ms.ToArray();
                mimeType = ExtensionMethods.GetMimeType(fileBytes, upload.Definition.FileName);
            }
            forbiddenMimeTypeList.ForEach(mime => {
                //We applied two verification, the first one is done on the typical mime given by the header of the request
                //and the second is done by retrieving the first bits of the file thanks to an extension method
                if (upload.Definition.MimeType == mime || mimeType == mime)
                { 
                    throw new Exception("This type of file is not allowed : " + mime);
                }
            });

            if(upload.Definition.MaxSize < 1)
            {
                long maxSizeByte = (long)ExtensionMethods.ConvertGBToLong(maxSize);
                upload.Definition.MaxSize = maxSizeByte;

                if (upload.File.Length > maxSizeByte)
                {
                    throw new Exception("the file is too large");
                }
            }

            string pathUpload = Path.Combine(_environment.WebRootPath, "uploadManager");
            string bodyHash = Guid.NewGuid().ToString();
            //Mechanics to encrypt the path of the uploaded file
            var pathResult = Path.Combine(pathUpload, bodyHash.Substring(0, 4), bodyHash);

            newObject = await SaveUpload(upload, pathResult, pathUpload, bodyHash);
            return "api/HAUpload/GetFile/" + newObject.Id;   
        }

        public async Task UploadBackUpAsync(UploadBackUpRequest upload)
        {
            await using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var sm = upload.File.OpenReadStream();
                var archive = new ZipArchive(sm);
                await Task.Run (() => archive.ExtractToDirectory(Path.Combine(_environment.WebRootPath, "uploadManager")));
            }
        }

        public async Task<bool> DeleteUploadAsync(int id)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAUploadDefinition upload = await apidbContext.HAUploadDefinitions.FindAsync(id);
                if (upload != null)
                {
                    apidbContext.HAUploadDefinitions.Remove(upload);
                    await apidbContext.SaveChangesAsync();

                    string[] files = Directory.GetFiles(Path.Combine(_environment.WebRootPath, "uploadManager", upload.Hash.Substring(0, 4)));
                    files.ToList().ForEach(file => File.Delete(file));

                    return true;
                }
                else
                    return false;
            }
        }

        public async Task<HAUploadDefinition> GetFileAsync(int id)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.HAUploadDefinitions.FindAsync(id);
            }
        }

        public async Task<List<HAUploadDefinition>> GetUploadListAsync()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.HAUploadDefinitions.ToListAsync();
            }
        }

        public async Task<Stream> GetUploadStream(int id)
        {
            HAUploadDefinition upload = new HAUploadDefinition();
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                upload = await apidbContext.HAUploadDefinitions.FindAsync(id);
            }

            string[] files = Directory.GetFiles(Path.Combine(_environment.WebRootPath, "uploadManager", upload.Hash.Substring(0, 4)));
            return new FileStream(files[0], FileMode.Open);
        }

        public async Task<int> UploadFileFromApiAsync(HAUploadDefinitionFromApi upload)
        {
            HAUploadDefinition newObject = new HAUploadDefinition();

            var forbiddenMimeTypeList = _configuration.GetSection("ApplicationSettings:UploadSettings:UploadForbiddenTypes").Get<List<string>>();
            var maxSize = _configuration.GetSection("ApplicationSettings:UploadSettings:UploadMaxSize").Get<string>();
            string mimeType = "";

            //Use byte array sequences to determine the correct MIME type of a given file. 
            using (var ms = new MemoryStream())
            {
                upload.UploadStream.CopyTo(ms);
                var fileBytes = ms.ToArray();
                mimeType = ExtensionMethods.GetMimeType(fileBytes, upload.Definition.FileName);
            }

            forbiddenMimeTypeList.ForEach(mime => {
                //We applied two verification, the first one is done on the typical mime given by the header of the request
                //and the second is done by retrieving the first bits of the file thanks to an extension method
                if (upload.Definition.MimeType == mime)
                {
                    if (mimeType == mime)
                        throw new Exception("This type of file is not allowed : " + mime);
                }
            });

            if (upload.Definition.MaxSize < 1)
            {
                long maxSizeByte = (long)ExtensionMethods.ConvertGBToLong(maxSize);
                upload.Definition.MaxSize = maxSizeByte;

                if (upload.UploadStream.Length > maxSizeByte)
                {
                    throw new Exception("the file is too large");
                }
            }

            string pathUpload = Path.Combine(_environment.WebRootPath, "uploadManager");
            string bodyHash = Guid.NewGuid().ToString();
            //Mechanics to encrypt the path of the uploaded file
            var pathResult = Path.Combine(pathUpload, bodyHash.Substring(0, 4), bodyHash);

            newObject = await SaveUpload(upload, pathResult, pathUpload, bodyHash);
            return newObject.Id;
        }

        public async Task<string> CompressFilesAsync()
        {
            List<HAUploadDefinition> listUploads = new List<HAUploadDefinition>();
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                listUploads =  await apidbContext.HAUploadDefinitions.ToListAsync();
            }
            List<string> filesToZip =  listUploads.Select(upload => upload.Path).ToList();

            // Create a temporary zip file
            string zipFilePath = Path.GetTempFileName() + ".zip";
            using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    foreach (string file in filesToZip)
                    {
                        archive.CreateEntryFromFile(file, Path.GetRelativePath(Path.Combine(_environment.WebRootPath, "uploadManager"), file));
                    }
                }
                return zipFilePath;
            }

        }

        public async Task<bool> DeleteFromRules()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                bool result = true;
                List<HAUploadDefinition> listUpload = await apidbContext.HAUploadDefinitions.ToListAsync();
                if (listUpload.Count > 0)
                {

                    foreach (HAUploadDefinition upload in listUpload)
                    {
                        if (upload.DayRetention > 0)
                        {
                            int days = upload.DayRetention;
                            TimeSpan ts = days * TimeSpan.FromDays(1);
                            DateTime deadline = upload.DateUpload.Add(ts);
                            if (deadline.ToString("dd/MM/yyyy") == DateTime.Now.ToString("dd/MM/yyyy"))
                            {
                                apidbContext.HAUploadDefinitions.Remove(upload);
                                await apidbContext.SaveChangesAsync();

                                string[] files = Directory.GetFiles(Path.Combine(_environment.WebRootPath, "uploadManager", upload.Hash.Substring(0, 4)));
                                files.ToList().ForEach(file => File.Delete(file));
                                foreach (var f in files)
                                {
                                    bool fileDeleteResult = true;
                                    try
                                    {
                                        File.Delete(f);
                                    }
                                    catch (Exception e)
                                    {
                                        fileDeleteResult = false;
                                    }

                                    result = result && fileDeleteResult;

                                    if (!result)
                                        break;
                                }
                            }
                        }
                    }
                    return result;
                }
                else
                {
                    result = false;
                    return result;
                }
            }
        }

        private async Task<HAUploadDefinition> SaveUpload(HAUploadDefinitionVM upload, string pathResult, string pathUpload, string bodyHash)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    HAUploadDefinition newObject = new HAUploadDefinition();
                    try
                    {
                        newObject.DateUpload = DateTime.Now;
                        newObject.Path = Path.GetFullPath(pathResult);
                        newObject.Hash = bodyHash;
                        newObject.Size = upload.File.Length;
                        newObject.MaxSize = upload.Definition.MaxSize;
                        newObject.FileName = upload.Definition.FileName;
                        newObject.DayRetention = upload.Definition.DayRetention;
                        newObject.SensitiveData = upload.Definition.SensitiveData;
                        newObject.MimeType = upload.Definition.MimeType;
                        newObject.Nature = upload.Definition.Nature;
                        // Perform database operations here
                        await apidbContext.HAUploadDefinitions.AddAsync(newObject);
                        await apidbContext.SaveChangesAsync();

                        //Create the directory to put the file uploaded after
                        Directory.CreateDirectory(Path.Combine(pathUpload, bodyHash.Substring(0, 4)));

                        //Upload of the file
                        using (var stream = new FileStream(pathResult, FileMode.Create))
                        {
                            await upload.File.CopyToAsync(stream);
                        }

                        // Commit the transaction
                        scope.Complete();

                        return newObject;
                    }
                    catch (TransactionException ex)
                    {
                        scope.Dispose();
                        throw new TransactionException(ex.Message);
                    }
                }
            }
        }
        private async Task<HAUploadDefinition> SaveUpload(HAUploadDefinitionFromApi upload, string pathResult, string pathUpload, string bodyHash)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    HAUploadDefinition newObject = new HAUploadDefinition();
                    try
                    {
                        newObject.DateUpload = DateTime.Now;
                        newObject.Path = Path.GetFullPath(pathResult);
                        newObject.Hash = bodyHash;
                        newObject.Size = upload.UploadStream.Length;
                        newObject.MaxSize = upload.Definition.MaxSize;
                        newObject.FileName = upload.Definition.FileName;
                        newObject.DayRetention = upload.Definition.DayRetention;
                        newObject.SensitiveData = upload.Definition.SensitiveData;
                        newObject.MimeType = upload.Definition.MimeType;
                        newObject.Nature = upload.Definition.Nature;
                        // Perform database operations here
                        await apidbContext.HAUploadDefinitions.AddAsync(newObject);
                        await apidbContext.SaveChangesAsync();

                        //Create the directory to put the file uploaded after
                        Directory.CreateDirectory(Path.Combine(pathUpload, bodyHash.Substring(0, 4)));
                        upload.UploadStream.Seek(0, SeekOrigin.Begin);
                        //Upload of the file
                        using (var stream = new FileStream(pathResult, FileMode.Create))
                        {
                            await upload.UploadStream.CopyToAsync(stream);
                        }

                        // Commit the transaction
                        scope.Complete();

                        return newObject;
                    }
                    catch (TransactionException ex)
                    {
                        scope.Dispose();
                        throw new TransactionException(ex.Message);
                    }
                }
            }
        }
    }
}
