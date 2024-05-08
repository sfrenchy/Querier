using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests;

namespace Querier.Api.Models.Interfaces
{
    public interface IHAUploadService
    {
        public Task<List<HAUploadDefinition>> GetUploadListAsync();
        public Task<HAUploadDefinition> GetFileAsync(int id);
        public Task<string> UploadFileFromVMAsync(HAUploadDefinitionVM upload);
        public Task<int> UploadFileFromApiAsync(HAUploadDefinitionFromApi upload);
        public Task UploadBackUpAsync(UploadBackUpRequest upload);
        public Task<bool> DeleteUploadAsync(int id);
        public Task<Stream> GetUploadStream(int id);
        public Task<string> CompressFilesAsync();
        public Task<bool> DeleteFromRules();
    }
}