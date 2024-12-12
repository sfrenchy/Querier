using System.Collections.Generic;
using Querier.Api.Domain.Common.ValueObjects;

namespace Querier.Api.Application.DTOs.Responses.Entity
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