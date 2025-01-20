using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Enums;

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

        public void ConfigureServices(IServiceCollection services, DbConnectionType connectionType, string connectionString, ILogger logger)
        {
            logger.LogDebug("[EntityServiceResolverTemplate] Configuring services for context: $contextNameSpace$");
            
            Action<DbContextOptionsBuilder> configureOptions = options =>
            {
                // Enable lazy loading and change tracking
                options.UseLazyLoadingProxies();
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
                options.UseLazyLoadingProxies(false);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                
                // Configure the database provider based on the connection string
                switch (connectionType)
                {
                    case DbConnectionType.SqlServer:
                        logger.LogDebug("[EntityServiceResolverTemplate] Using SQL Server provider");
                        options.UseSqlServer(connectionString);
                        break;
                    case DbConnectionType.MySql:
                        logger.LogDebug("[EntityServiceResolverTemplate] Using MySQL provider");
                        var serverVersion = new MariaDbServerVersion(new Version(10, 3, 9));
                        options.UseMySql(connectionString, serverVersion);
                        break;
                    case DbConnectionType.PgSql:
                        logger.LogDebug("[EntityServiceResolverTemplate] Using PostgreSQL provider");
                        options.UseNpgsql(connectionString);
                        break;
                    default:
                        logger.LogWarning("[EntityServiceResolverTemplate] Defaulting to SQL Server provider");
                        options.UseSqlServer(connectionString);
                        break;
                \}
            \};

            // Register the DbContext factory
            services.AddPooledDbContextFactory<$contextNameSpace$>(configureOptions);

            // Register the DbContext itself with scoped lifetime
            services.AddDbContext<$contextNameSpace$>(configureOptions);

            // Register DbContextOptions
            services.AddSingleton(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<$contextNameSpace$>();
                configureOptions(optionsBuilder);
                return optionsBuilder.Options;
            });

            foreach (var service in _entityServices)
            {
                logger.LogDebug("[EntityServiceResolverTemplate] AddTransient to service: " + service.Key + " -> " + service.Value);
                services.AddTransient(service.Key, service.Value);
            \}
        }

        public string DynamicContextName => "$contextNameSpace$";
    }
} 