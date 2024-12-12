using System.Collections.Generic;

namespace Querier.Api.Models.Interfaces
{
    public interface IDynamicContextList
    {
        public Dictionary<string, IDynamicContextProceduresServicesResolver> DynamicContexts { get; }
    }
    
}