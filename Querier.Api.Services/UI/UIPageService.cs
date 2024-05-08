using System.Text;
using DocumentFormat.OpenXml.Bibliography;
using Querier.Api.Models;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Requests;
using Querier.Api.Models.Responses;
using Querier.Api.Models.UI;
using Querier.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Querier.Api.Models.Enums;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Querier.Api.Services.UI
{
    public interface IUIPageService
    {
        PageManagementResponse Index();
        Task<HAPageVM> GetPageAsync(int? pageId);
        Task<List<HAPage>> GetPagesAsync();
        Task<IActionResult> GetAllPagesDatatableAsync(ServerSideRequest datatableRequest);
        Task<HAPage> AddPageAsync(AddPageRequest model);
        Task<HAPage> DeletePageAsync(int pageId);
        Task<HAPage> EditPageAsync(EditPageRequest model);
        Task<HAPage> DuplicatePageAsync(int pageId);
        Task CopyContextAlhPage(HAPage source);
        Task<HAPage> ExportPage(int pageId);
        Task<ExportPageResponse> ExportPageConfigurationAsync(ExportPageRequest exportPageRequest);
        Task<ExportPageResponse> ImportPageConfigurationAsync(PageImportConfigRequest pageImportConfigRequest);
    }
    public class UIPageService : IUIPageService
    {
        private readonly ILogger<UIPageService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _contextFactory;
        private readonly IHAUploadService _uploadService;
        private readonly IToastMessageEmitterService _toastMessageEmitterService;


        public UIPageService(ILogger<UIPageService> logger, IDbContextFactory<ApiDbContext> contextFactory, IHAUploadService uploadService, IToastMessageEmitterService toastMessageEmitterService)
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
                    Categories = apidbContext.HAPageCategories.Select(hac => new HAPageCategory()
                    {
                        Icon = hac.Icon == "" ? "circle" : hac.Icon,
                        Description = hac.Description,
                        HAPages = hac.HAPages,
                        Id = hac.Id,
                        Label = hac.Label,
                        HACategoryRoles = hac.HACategoryRoles
                    }).ToList(),
                    Pages = apidbContext.HAPages.Select(hap => new HAPage()
                    {
                        Icon = hap.Icon == "" ? "circle" : hap.Icon,
                        Description = hap.Description,
                        Id = hap.Id,
                        Title = hap.Title,
                        HAPageCategory = hap.HAPageCategory,
                        HAPageRoles = hap.HAPageRoles,
                        HAPageCategoryId = hap.HAPageCategoryId,
                        HAPageRows = hap.HAPageRows
                    }).ToList()
                };
                return model;
            }
        }

        public async Task<HAPageVM> GetPageAsync(int? pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                pageId ??= 1;
                HAPage page = await apidbContext.HAPages.FindAsync(pageId);
                return HAPageVM.FromHAPage(page);
            }
        }

        public async Task<List<HAPage>> GetPagesAsync()
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                return await apidbContext.HAPages.ToListAsync();
            }
        }

        public async Task<IActionResult> GetAllPagesDatatableAsync(ServerSideRequest datatableRequest)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                ServerSideResponse<HAPage> response = new ServerSideResponse<HAPage>();
                List<HAPage> result = await apidbContext.HAPages.ToListAsync();

                response.sums = null;
                response.draw = datatableRequest.draw;
                response.data = result;
                response.recordsFiltered = result.Count;
                response.recordsTotal = result.Count;

                return new JsonResult(new { response });
            }
        }

        public async Task<HAPage> AddPageAsync(AddPageRequest model)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                var pageEntity = new HAPage()
                {
                    HAPageCategoryId = model.CategoryId,
                    Title = model.PageTitle,
                    Description = model.PageDescription,
                    Icon = model.PageIcon
                };

                await apidbContext.HAPages.AddAsync(pageEntity);
                IEnumerable<HAPageRole> EnumerableNewPagesRoles = model.IdRoles.Select(IdRole => new HAPageRole() { 
                    HAPage = pageEntity, 
                    ApiRoleId = IdRole, 
                    View = true,
                    Add = true,
                    Edit = true,
                    Remove = true
                });

                await apidbContext.HAPageRoles.AddRangeAsync(EnumerableNewPagesRoles);

                await apidbContext.SaveChangesAsync();

                return pageEntity;
            }
        }

        public async Task<HAPage> DeletePageAsync(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPage page = await apidbContext.HAPages.FindAsync(pageId);
                if (page != null)
                {
                    apidbContext.HAPages.Remove(page);
                    await apidbContext.SaveChangesAsync();
                }

                return page;
            }
        }

        public async Task<HAPage> EditPageAsync(EditPageRequest model)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPage updatePage = await apidbContext.HAPages.FindAsync(model.PageId);

                updatePage.Title = model.PageTitle;
                updatePage.Description = model.PageDescription;
                updatePage.HAPageCategoryId = model.CategoryId;
                updatePage.Icon = model.PageIcon;

                await apidbContext.SaveChangesAsync();

                return updatePage;
            }
        }

        public async Task<HAPage> DuplicatePageAsync(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPage duplicatePageSource = await apidbContext.HAPages.FindAsync(pageId);

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
        public async Task CopyContextAlhPage(HAPage source)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPage newEntity = source.EntityCopy();
                newEntity.Title = source.Title + "(copie)";

                apidbContext.Add(newEntity);
                await apidbContext.SaveChangesAsync();
            }
        }

        public async Task<HAPage> ExportPage(int pageId)
        {
            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                HAPage source = await apidbContext.HAPages.FindAsync(pageId);
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

            HAPage sourceToExport = new HAPage();
            byte[] content;

            using (var apidbContext = _contextFactory.CreateDbContext())
            {
                sourceToExport = await apidbContext.HAPages.FindAsync(exportPageRequest.PageId);            

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
                serializeOptions.Converters.Add(new IgnoreHAPageCardConfigurationPropertyConverter<HAPageCard>());
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
                    Nature = HAUploadNatureEnum.PageConfiguration
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

            HAPage importPage = JsonSerializer.Deserialize<HAPage>(json);
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

                if (typeof(T) == typeof(HAPageCard) && property.Name == "Configuration")
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
