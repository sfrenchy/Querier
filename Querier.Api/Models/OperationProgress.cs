using System;

namespace Querier.Api.Models
{
    public class OperationProgress
    {
        public string OperationId { get; set; }
        public int Progress { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 