﻿using System.Threading.Tasks;
using Querier.Api.Models;
using Querier.Api.Models.Interfaces;
using Querier.Api.Models.Notifications.MQMessages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Querier.Api.Tools;

namespace Querier.Api.Quartz
{
    public class DeleteUploadJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            using (var serviceScope = ServiceActivator.GetScope())
            {
                var uploadService = serviceScope.ServiceProvider.GetService<IQUploadService>();
                var toastMessageEmitterService = serviceScope.ServiceProvider.GetService<IToastMessageEmitterService>();
                var logger = serviceScope.ServiceProvider.GetService<ILogger<DeleteUploadJob>>();

                ToastMessage message = new ToastMessage();
                message.Recipient = "admin@querier.fr";
                message.Persistent = false;
                if (await uploadService.DeleteFromRules())
                {
                    // message.TitleCode = "lbl-title-toast-delete-job-success";
                    // message.ContentCode = "lbl-content-toast-delete-job-success";
                    // message.Type = ToastType.Success;
                    // toastMessageEmitterService.PublishToast(message);
                }
                else
                {
                    // message.TitleCode = "lbl-title-toast-delete-job-error";
                    // message.ContentCode = "lbl-content-toast-delete-job-error";
                    // message.Type = ToastType.Danger;
                    // toastMessageEmitterService.PublishToast(message);
                    // logger.LogError("An error occurred while deleting some uploads");
                }
            }
        }
    }
}
