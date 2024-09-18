using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Bibliography;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Requests;
using Querier.Api.Models.Responses;
using Querier.Api.Models.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Querier.Api.Tools;

namespace Querier.Api.Services.UI
{
    public interface IUIPageService
    {
        PageManagementResponse Index();
        Task<QPageVM> GetPageAsync(int? pageId);
        Task<List<QPage>> GetPagesAsync();
        Task<IActionResult> GetAllPagesDatatableAsync(ServerSideRequest datatableRequest);
        Task<QPage> AddPageAsync(AddPageRequest model);
        Task<QPage> DeletePageAsync(int pageId);
        Task<QPage> EditPageAsync(EditPageRequest model);
        Task<QPage> DuplicatePageAsync(int pageId);
        Task CopyContextAlhPage(QPage source);
        Task<QPage> ExportPage(int pageId);
        Task<ExportPageResponse> ExportPageConfigurationAsync(ExportPageRequest exportPageRequest);
        Task<ExportPageResponse> ImportPageConfigurationAsync(PageImportConfigRequest pageImportConfigRequest);
    }
    public class UIPageService : IUIPageService
    {
        private readonly ILogger<UIPageService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly Models.Interfaces.IQUploadService _uploadService;
        private readonly IToastMessageEmitterService _toastMessageEmitterService;


        public UIPageService(ILogger<UIPageService> logger, IDbContextFactory<ApiDbContext> contextFactory, Models.Interfaces.IQUploadService uploadService, IToastMessageEmitterService toastMessageEmitterService)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _uploadService = uploadService;
            _toastMessageEmitterService = toastMessageEmitterService;
        }

        public PageManagementResponse Index()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                PageManagementResponse model = new PageManagementResponse
                {
                    Categories = apidbContext.QPageCategories.Select(hac => new QPageCategory()
                    {
                        Icon = hac.Icon == "" ? "circle" : hac.Icon,
                        Description = hac.Description,
                        QPages = hac.QPages,
                        Id = hac.Id,
                        Label = hac.Label,
                        QCategoryRoles = hac.QCategoryRoles
                    }).ToList(),
                    Pages = apidbContext.QPages.Select(hap => new QPage()
                    {
                        Icon = hap.Icon == "" ? "circle" : hap.Icon,
                        Description = hap.Description,
                        Id = hap.Id,
                        Title = hap.Title,
                        QPageCategory = hap.QPageCategory,
                        QPageRoles = hap.QPageRoles,
                        HAPageCategoryId = hap.HAPageCategoryId,
                        QPageRows = hap.QPageRows
                    }).ToList()
                };
                return model;
            }
        }

        public async Task<QPageVM> GetPageAsync(int? pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                pageId ??= 1;
                QPage page = await apidbContext.QPages.FindAsync(pageId);
                return QPageVM.FromHAPage(page);
            }
        }

        public async Task<List<QPage>> GetPagesAsync()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.QPages.ToListAsync();
            }
        }

        public async Task<IActionResult> GetAllPagesDatatableAsync(ServerSideRequest datatableRequest)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                ServerSideResponse<QPage> response = new ServerSideResponse<QPage>();
                List<QPage> result = await apidbContext.QPages.ToListAsync();

                response.sums = null;
                response.draw = datatableRequest.draw;
                response.data = result;
                response.recordsFiltered = result.Count;
                response.recordsTotal = result.Count;

                return new JsonResult(new { response });
            }
        }

        public async Task<QPage> AddPageAsync(AddPageRequest model)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var pageEntity = new QPage()
                {
                    HAPageCategoryId = model.CategoryId,
                    Title = model.PageTitle,
                    Description = model.PageDescription,
                    Icon = model.PageIcon
                };

                await apidbContext.QPages.AddAsync(pageEntity);
                IEnumerable<QPageRole> EnumerableNewPagesRoles = model.IdRoles.Select(IdRole => new QPageRole() { 
                    QPage = pageEntity, 
                    ApiRoleId = IdRole, 
                    View = true,
                    Add = true,
                    Edit = true,
                    Remove = true
                });

                await apidbContext.QPageRoles.AddRangeAsync(EnumerableNewPagesRoles);

                await apidbContext.SaveChangesAsync();

                return pageEntity;
            }
        }

        public async Task<QPage> DeletePageAsync(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPage page = await apidbContext.QPages.FindAsync(pageId);
                if (page != null)
                {
                    apidbContext.QPages.Remove(page);
                    await apidbContext.SaveChangesAsync();
                }

                return page;
            }
        }

        public async Task<QPage> EditPageAsync(EditPageRequest model)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPage updatePage = await apidbContext.QPages.FindAsync(model.PageId);

                updatePage.Title = model.PageTitle;
                updatePage.Description = model.PageDescription;
                updatePage.HAPageCategoryId = model.CategoryId;
                updatePage.Icon = model.PageIcon;

                await apidbContext.SaveChangesAsync();

                return updatePage;
            }
        }

        public async Task<QPage> DuplicatePageAsync(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPage duplicatePageSource = await apidbContext.QPages.FindAsync(pageId);

                if (duplicatePageSource != null)
                {
                    await CopyContextAlhPage(duplicatePageSource);
                }

                return duplicatePageSource;
            }
        }

        /// <summary>
        /// Used to copy an entity
        /// </summary>
        /// <param name="source">The entity to copy</param>
        public async Task CopyContextAlhPage(QPage source)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPage newEntity = source.EntityCopy();
                newEntity.Title = source.Title + "(copie)";

                apidbContext.Add(newEntity);
                await apidbContext.SaveChangesAsync();
            }
        }

        public async Task<QPage> ExportPage(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                QPage source = await apidbContext.QPages.FindAsync(pageId);
                return source.EntityCopy();
            }
        }

        /// <summary>
        /// Used to export a page configuration 
        /// </summary>
        /// <param name="exportPageRequest">The request with the page to copy</param>
        public async Task<ExportPageResponse> ExportPageConfigurationAsync(ExportPageRequest exportPageRequest)
        {
            _logger.LogInformation("Generating export page configuration");

            string bodyHash = Guid.NewGuid().ToString() + ".dat";

            QPage sourceToExport = new QPage();
            byte[] content;

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                sourceToExport = await apidbContext.QPages.FindAsync(exportPageRequest.PageId);            

                if (sourceToExport == null)
                {
                    return new ExportPageResponse() 
                    { 
                        Message = $"The page is not available for export",
                        Success = false
                    };
                }

                var serializeOptions = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default
                };
                serializeOptions.Converters.Add(new IgnoreHAPageCardConfigurationPropertyConverter<QPageCard>());
                var pageToExport = sourceToExport.EntityCopy();
                var jsonPage = JsonSerializer.Serialize(pageToExport, serializeOptions);
                content = Encoding.UTF8.GetBytes(jsonPage);
            }

            HAUploadDefinitionFromApi uploadDef = new HAUploadDefinitionFromApi()
            {
                Definition = new SimpleUploadDefinition()
                {
                    FileName = bodyHash,
                    MimeType = "application/octet-stream",
                    DayRetention = 1,
                    Nature = QUploadNatureEnum.PageConfiguration
                },
                UploadStream = new MemoryStream(content)
            };

            int idUpload = await _uploadService.UploadFileFromApiAsync(uploadDef);

            string downloadURL = $"api/HAUpload/GetFile/{idUpload}";

            ToastMessage exportAvailableMessage = new ToastMessage();
            exportAvailableMessage.TitleCode = "lbl-export-page-configuration-available-title";
            exportAvailableMessage.Recipient = exportPageRequest.RequestUserEmail;
            exportAvailableMessage.ContentCode = "lbl-export-page-configuration-available-content";
            exportAvailableMessage.ContentDownloadURL = downloadURL;
            exportAvailableMessage.ContentDownloadsFilename = $"{exportPageRequest.FileTitle}.dat";
            exportAvailableMessage.Closable = true;
            exportAvailableMessage.Persistent = true;
            exportAvailableMessage.Type = ToastType.Success;
            _logger.LogInformation("Publishing export page configuration notification");
            _toastMessageEmitterService.PublishToast(exportAvailableMessage);

            return new ExportPageResponse()
            { 
                Message = $"The page configuration is available to download",
                Success = true
            };
        }

        /// <summary>
        /// Used to impo a page configuration 
        /// </summary>
        public async Task<ExportPageResponse> ImportPageConfigurationAsync(PageImportConfigRequest pageImportConfigRequest)
        {
            byte[] fileContent;
            int pageId;
            string json = "";
            using (StreamReader r = new StreamReader(pageImportConfigRequest.FilePath, Encoding.UTF8))
            {
                json = r.ReadToEnd();
            }

            QPage importPage = JsonSerializer.Deserialize<QPage>(json);
            importPage.HAPageCategoryId = pageImportConfigRequest.CategoryId;
            importPage.Title = $"{importPage.Title} copie";

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                try
                {
                    apidbContext.Add(importPage);
                    await apidbContext.SaveChangesAsync();

                    return new ExportPageResponse()
                    {
                        Message = "Page successfully created in database",
                        Id = importPage.Id,
                        Success = true
                    };
                }
                catch (Exception e)
                {
                    return new ExportPageResponse()
                    {
                        Message = e.Message
                    };
                }
            }
        }

    }
    
    public class IgnoreHAPageCardConfigurationPropertyConverter<T> : JsonConverter<T>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(T) == typeToConvert;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                bool ignore = false;

                if (typeof(T) == typeof(QPageCard) && property.Name == "Configuration")
                    ignore = true;
                
                if (!ignore)
                {
                    writer.WritePropertyName(property.Name);
                    JsonSerializer.Serialize(writer, property.GetValue(value), options);
                }
            }

            writer.WriteEndObject();
        }
    }
}
