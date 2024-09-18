using Querier.Api.Models.Notifications.MQMessages;

namespace Querier.Api.Models.Requests
{
    public class ImportEntitiesFromCSVRequest : MQMessage
    {
        public string requestUserEmail { get; set; }
        public bool allowUpdate { get; set; }
        public string identifierColumn { get; set; }
        public string contextType { get; set; }
        public string entityType { get; set; }
        public string filePath { get; set; }
    }
}