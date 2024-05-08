using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Querier.Api.Models.Interfaces
{
    public interface IDynamicContextProceduresServicesResolver
    {
        public Dictionary<Type, Type> ProceduresServices { get; }
        public Dictionary<string, Type> ProcedureNameService { get; }

        public void ConfigureServices(IServiceCollection services, string connectionString);
        public string DynamicContextName { get; }
    }
}