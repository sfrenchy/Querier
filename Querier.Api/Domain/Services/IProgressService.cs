using System.Threading.Tasks;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Domain.Services
{
    /// <summary>
    /// Service for tracking and reporting progress of long-running operations
    /// </summary>
    public interface IProgressService
    {
        /// <summary>
        /// Starts tracking a new operation
        /// </summary>
        /// <param name="operationId">Unique identifier for the operation</param>
        /// <param name="status">Initial status</param>
        Task StartOperation(string operationId, ProgressStatus status);

        /// <summary>
        /// Reports progress for an ongoing operation
        /// </summary>
        /// <param name="operationId">Operation identifier</param>
        /// <param name="progress">Progress percentage (0-100)</param>
        /// <param name="status">Current status</param>
        Task ReportProgress(string operationId, int progress, ProgressStatus status);

        /// <summary>
        /// Marks an operation as completed
        /// </summary>
        /// <param name="operationId">Operation identifier</param>
        /// <param name="status">Final status</param>
        Task CompleteOperation(string operationId, ProgressStatus status);

        /// <summary>
        /// Marks an operation as failed
        /// </summary>
        /// <param name="operationId">Operation identifier</param>
        /// <param name="status">Error status</param>
        Task FailOperation(string operationId, ProgressStatus status);
    }
} 