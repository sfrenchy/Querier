using Querier.Api.Models;
using Querier.Api.Models.Common;
using Querier.Api.Models.Enums.Ged;
using Querier.Api.Models.Ged;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Services.Ged;

namespace Querier.Api.Services.Factory
{
    public class FileDepositFactory
    {
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly ILogger<FileSystemService> _loggerFileSystem;
        private readonly ILogger<GedDocuwareService> _loggerGedDocuware;
        private readonly IHAUploadService _uploadService;

        public FileDepositFactory(IDbContextFactory<ApiDbContext> contextFactory, ILogger<FileSystemService> loggerFileSystem, ILogger<GedDocuwareService> loggerGedDocuware, IHAUploadService uploadService)
        {
            _contextFactory = contextFactory;
            _loggerFileSystem = loggerFileSystem;
            _loggerGedDocuware = loggerGedDocuware;
            _uploadService = uploadService;
        }

        public IHAFileReadOnlyDeposit? CreateClassInstanceByTag(string tag)
        {
            HAFileDeposit fileDeposit;
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                fileDeposit = apidbContext.HAFileDeposit.FirstOrDefault(r => r.Tag == tag);
            }
            return CreateInstance(fileDeposit);
        }

        public IHAFileReadOnlyDeposit? CreateClassInstanceByType(TypeFileDepositEnum typeFileDeposit)
        {
            HAFileDeposit fileDeposit;
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                fileDeposit = apidbContext.HAFileDeposit.FirstOrDefault(r => r.Type == typeFileDeposit);
            }

            return CreateInstance(fileDeposit);
        }
        private IHAFileReadOnlyDeposit? CreateInstance(HAFileDeposit fileDeposit)
        {
            if(fileDeposit == null)
            {
                return null;
            }
            switch (fileDeposit.Type)
            {
                case TypeFileDepositEnum.FileSystem:
                    return new FileSystemService(_loggerFileSystem, _contextFactory, _uploadService);
                case TypeFileDepositEnum.Docuware:
                    return new GedDocuwareService(_loggerGedDocuware, _contextFactory);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
