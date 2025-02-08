using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Models;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Domain.Services
{
    /// <summary>
    /// Implementation of the progress tracking service
    /// </summary>
    public class ProgressService : IProgressService
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<ProgressService> _logger;

        public ProgressService(
            INotificationService notificationService,
            ILogger<ProgressService> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task StartOperation(string operationId, ProgressStatus status)
        {
            _logger.LogInformation("Starting operation {OperationId} with status: {Status}", 
                operationId, status);

            var progress = new ProgressEvent
            {
                OperationId = operationId,
                Progress = 0,
                Status = status
            };

            await _notificationService.SendOperationProgressAsync(operationId, progress);
        }

        public async Task ReportProgress(string operationId, int progress, ProgressStatus status)
        {
            if (progress < 0 || progress > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(progress), 
                    "Progress must be between 0 and 100");
            }

            _logger.LogDebug("Operation {OperationId} progress: {Progress}% - {Status}", 
                operationId, progress, status);

            var progressEvent = new ProgressEvent
            {
                OperationId = operationId,
                Progress = progress,
                Status = status
            };

            await _notificationService.SendOperationProgressAsync(operationId, progressEvent);
        }

        public async Task CompleteOperation(string operationId, ProgressStatus status)
        {
            _logger.LogInformation("Operation {OperationId} completed: {Status}", 
                operationId, status);

            var progress = new ProgressEvent
            {
                OperationId = operationId,
                Progress = 100,
                Status = status
            };

            await _notificationService.SendOperationProgressAsync(operationId, progress);
        }

        public async Task FailOperation(string operationId, ProgressStatus status)
        {
            _logger.LogError("Operation {OperationId} failed: {Status}", 
                operationId, status);

            var progress = new ProgressEvent
            {
                OperationId = operationId,
                Progress = -1,
                Status = status
            };

            await _notificationService.SendOperationProgressAsync(operationId, progress);
        }
    }
} 