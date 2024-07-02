using Querier.Api.Models;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Requests.Ged;
using Querier.Api.Models.Responses;
using Querier.Api.Models.Responses.Ged;
using Querier.Api.Models.UI;
using Querier.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Querier.Api.Models.Common;
using Querier.Api.Models.Ged;

namespace Querier.Api.Services.Ged
{
    public interface IFileDepositService
    {
        //this function is used to get all file deposit from the table QFileDeposit, used for datatable
        Task<ServerSideResponse<FileDepositResponse>> GetAllFileDeposit(ServerSideRequest request);
        //this function is used to delete a file deposit with the id is given in parameter
        Task<GeneralResponse> DeleteFileDeposit(int fileDepositId);
        //this function is used to update a existing file deposit
        Task<GeneralResponse> UpdateFileDeposit(FileDepositRequest FileDepositToUpdate);
        //this function is used for add a new file deposit
        Task<GeneralResponse> AddFileDeposit(FileDepositRequest FileDepositToAdd);
        Task<List<FileDepositResponse>> GetAllFileDepositActive();

    }
    public class FileDepositService : IFileDepositService
    {
        private readonly ILogger<FileDepositService> _logger;
        private readonly IDbContextFactory<ApiDbContext> _apiDbContextFactory;

        public FileDepositService(ILogger<FileDepositService> logger, IDbContextFactory<ApiDbContext> apiDbContextFactory)
        {
            _logger = logger;
            _apiDbContextFactory = apiDbContextFactory;
        }

        public async Task<ServerSideResponse<FileDepositResponse>> GetAllFileDeposit(ServerSideRequest request)
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                ServerSideResponse<FileDepositResponse> r = new ServerSideResponse<FileDepositResponse>();
                r.data = apiDbContext.QFileDeposit.Select(c => new FileDepositResponse()
                {
                    Id = c.Id,
                    Auth = c.Auth,
                    Capabilities = c.Capabilities,
                    Enable = c.Enable,
                    Filter = c.Filter,
                    Host = c.Host,
                    Label = c.Label,
                    Login = c.Login,
                    Password = c.Password,
                    Port = c.Port,
                    RootPath =c.RootPath,
                    Tag = c.Tag,
                    Type = c.Type
                }).DatatableFilter(request, out int? countFiltered).ToList();
                r.draw = request.draw;
                r.recordsTotal = apiDbContext.QFileDeposit.Count();
                r.recordsFiltered = (int)countFiltered;
                
                return r;
            }
        }

        public async Task<GeneralResponse> DeleteFileDeposit(int fileDepositId)
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                QFileDeposit filedepositToRemove = await apiDbContext.QFileDeposit.FirstAsync(c => c.Id == fileDepositId);
                if (filedepositToRemove == null)
                {
                    return new GeneralResponse() { success = false, message = "file deposit not find" };
                }
                apiDbContext.QFileDeposit.Remove(filedepositToRemove);
                await apiDbContext.SaveChangesAsync();
                return new GeneralResponse() { success = true, message = "file deposit has been deleted" };
            }
        }

        public async Task<GeneralResponse> UpdateFileDeposit(FileDepositRequest FileDepositToUpdate)
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                QFileDeposit fileDepositOrigin = await apiDbContext.QFileDeposit.FirstAsync(c => c.Id == FileDepositToUpdate.Id);
                if (fileDepositOrigin == null)
                {
                    return new GeneralResponse() { success = false, message = "file deposit not find" };
                }

                // compare properties with reflexion 

                // used to find which variable between FileDepositToUpdate and fileDepositOrigin does not have the property 
                bool IHaveValueFromObject = false;
                bool IHaveValueFromEntity = false;
                //for detect if at least one field has been change 
                bool columnChange = false;

                PropertyInfo[] properties = typeof(FileDepositRequest).GetProperties();
                foreach (var property in properties)
                {
                    try
                    {
                        PropertyInfo p = typeof(QFileDeposit).GetProperty(property.Name);
                        var valueFromObject = property.GetValue(FileDepositToUpdate);
                        IHaveValueFromObject = true;
                        var valueFromEntity = p.GetValue(fileDepositOrigin);
                        IHaveValueFromEntity = true;

                        if (!Equals(valueFromObject, valueFromEntity))
                        {
                            p.SetValue(fileDepositOrigin, valueFromObject);
                            columnChange = true;
                        }
                        IHaveValueFromObject = false;
                        IHaveValueFromEntity = false;
                    }
                    catch(Exception ex)
                    {
                        if (!IHaveValueFromObject)
                        {
                            throw new Exception($"Property \"{property}\" does not exist in variable {FileDepositToUpdate}.");
                        }
                        if (!IHaveValueFromEntity)
                        {
                            throw new Exception($"Property \"{property}\" does not exist in variable {fileDepositOrigin}.");
                        }
                    }
                }
                if (columnChange)
                {
                    await apiDbContext.SaveChangesAsync();
                    return new GeneralResponse() { success = true, message = "file deposit updated successfully" };
                }
                return new GeneralResponse() { success = false, message = "no change has been detected" };
            }
        }

        public async Task<GeneralResponse> AddFileDeposit(FileDepositRequest FileDepositToAdd)
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                QFileDeposit newFileDeposit = new QFileDeposit()
                {
                    Auth = FileDepositToAdd.Auth,
                    Capabilities = FileDepositToAdd.Capabilities,
                    Enable = FileDepositToAdd.Enable,
                    Filter = FileDepositToAdd.Filter,
                    Host = FileDepositToAdd.Host,
                    Label = FileDepositToAdd.Label,
                    Login = FileDepositToAdd.Login,
                    Password = FileDepositToAdd.Password,
                    Port = FileDepositToAdd.Port,
                    RootPath = FileDepositToAdd.RootPath,
                    Tag = FileDepositToAdd.Tag,
                    Type = FileDepositToAdd.Type,
                };
                apiDbContext.QFileDeposit.Add(newFileDeposit);
                await apiDbContext.SaveChangesAsync();

                return new GeneralResponse() { success = true, message = "file deposit has been added" };
            }
        }

        public async Task<List<FileDepositResponse>> GetAllFileDepositActive()
        {
            using (var apiDbContext = await _apiDbContextFactory.CreateDbContextAsync())
            {
                List<QFileDeposit> fileDepositActif = apiDbContext.QFileDeposit.Where(f => f.Enable == true).ToList();
                List<FileDepositResponse> result = fileDepositActif.Select(f => new FileDepositResponse() { Auth = f.Auth, Capabilities = f.Capabilities, Filter = f.Filter, Host = f.Host, Label = f.Label, Login = f.Login, Enable = f.Enable, Id = f.Id, Password = f.Password, Port = f.Port, RootPath = f.RootPath, Tag = f.Tag, Type = f.Type }).ToList();
                return result;
            }
        }
    }
}
