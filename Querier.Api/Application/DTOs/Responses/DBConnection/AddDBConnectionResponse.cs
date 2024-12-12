using System.Collections.Generic;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Application.DTOs.Responses.DBConnection
{
    public class AddDBConnectionResponse
    {
        public bool IsInError { get; set; } = false;
        public QDBConnectionState State { get; set; } = QDBConnectionState.None;
        public List<string> Messages { get; set; } = new List<string>();
    }
}