using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace Querier.Api.Models.Requests
{
    public class UploadBackUpRequest
    {
        public IFormFile File { get; set; }
    }
}
