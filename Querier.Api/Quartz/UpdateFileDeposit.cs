using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Responses.Ged;
using Querier.Api.Services.Factory;
using Querier.Api.Services.Ged;
using Querier.Tools;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Querier.Api.Quartz
{
    public class UpdateFileDeposit : IJob
    {

        public async Task Execute(IJobExecutionContext context)
        {
            using (var serviceScope = ServiceActivator.GetScope())
            {
                JobKey key = context.JobDetail.Key;
                var creator = context.MergedJobDataMap.GetString("Creator");

                IFileDepositService fileDepositSrv = serviceScope.ServiceProvider.GetService<IFileDepositService>();
                var fileDepositFactory = serviceScope.ServiceProvider.GetService<FileDepositFactory>();

                Task<List<FileDepositResponse>> fileDepositActive = fileDepositSrv.GetAllFileDepositActive();
                if(fileDepositActive.Result.Count() != 0)
                {
                    foreach(var fileDeposit in fileDepositActive.Result)
                    {
                        IQFileReadOnlyDeposit fileDepositInstance = fileDepositFactory.CreateClassInstanceByType(fileDeposit.Type);
                        Task<FillFileInformationResponse> result = fileDepositInstance.FillFileInformations();
                        await Console.Out.WriteLineAsync("Instance " + key + "  --- Update file deposit " + fileDeposit.Label + " "+ result.Result.numberFileAdded + "files added --- the creator is " + creator);
                    }
                }
                else
                {
                    await Console.Out.WriteLineAsync("Instance " + key + "  --- Update file deposit: 0 file deposit active found --- the creator is " + creator);
                }
            }
        }
    }
}
