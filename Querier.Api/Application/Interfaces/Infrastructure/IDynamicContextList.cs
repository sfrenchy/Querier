using System.Collections.Generic;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    public interface IDynamicContextList
    {
        public Dictionary<string, IDynamicContextProceduresServicesResolver> DynamicContexts { get; }
    }

}