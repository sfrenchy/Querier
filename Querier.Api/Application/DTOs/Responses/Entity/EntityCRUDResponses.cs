using System.Collections.Generic;
using Querier.Api.Models.Common;

namespace Querier.Api.Models.Responses
{

    public class CRUDCreateOrUpdateResponse
    {
        /// <summary>
        /// The newly created/updated entity
        /// </summary>
        public dynamic NewEntity { get; set; }
    }
    public class CRUDGetEntitiesResponse
    {
        public List<EntityDefinition> Entities { get; set; }
    }
}