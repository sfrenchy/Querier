using System;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Domain.Models
{
    /// <summary>
    /// Represents a progress event for long-running operations
    /// </summary>
    public class ProgressEvent
    {
        /// <summary>
        /// Unique identifier for the operation
        /// </summary>
        public string OperationId { get; set; }

        /// <summary>
        /// Current progress percentage (0-100)
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Current status message
        /// </summary>
        public ProgressStatus Status { get; set; }

        /// <summary>
        /// Optional error message if operation failed
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Timestamp of the event
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
} 