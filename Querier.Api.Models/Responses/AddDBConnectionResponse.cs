using System.Collections.Generic;
using Querier.Api.Models.Enums;

namespace Querier.Api.Models.Responses
{
    public class AddDBConnectionResponse
    {
        public bool IsInError { get; set; } = false;
        public QDBConnectionState State { get; set; } = QDBConnectionState.None;
        public List<string> Messages { get; set; } = new List<string>();
    }
}