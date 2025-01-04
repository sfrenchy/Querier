using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Infrastructure;

namespace $nameSpace$.Services
{
    public class $contextNameSpace$EntityServicesResolver : IDynamicContextEntityServicesResolver
    {
        private readonly Dictionary<Type, Type> _entityServices = new Dictionary<Type, Type>();
        private readonly Dictionary<string, Type> _entityNameService = new Dictionary<string, Type>();

        public $contextNameSpace$EntityServicesResolver()
        {
            $entityList: {entity|
                _entityServices.Add(typeof(I$entity.Name$Service), typeof($entity.Name$Service));
                _entityNameService.Add("$entity.Name$", typeof(I$entity.Name$Service));
            }$
        \}

        public Dictionary<Type, Type> EntityServices => _entityServices;
        public Dictionary<string, Type> EntityNameService => _entityNameService;

        public void ConfigureServices(IServiceCollection services, string connectionString)
        {
            services.AddPooledDbContextFactory<$contextNameSpace$>(options => 
                options.UseSqlServer(connectionString));

            foreach (var service in _entityServices)
            {
                services.AddScoped(service.Key, service.Value);
            \}
        }

        public string DynamicContextName => "$contextNameSpace$";
    }
} 