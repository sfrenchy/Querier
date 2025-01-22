using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using $nameSpace$.Exceptions;
using $nameSpace$.Repositories;
using $nameSpace$.Services;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Common.Enums;

namespace $nameSpace$.Services
{
    /// <summary>
    /// Resolver for dynamic entity services in the $contextNameSpace$ context
    /// </summary>
    public class $contextNameSpace$EntityServicesResolver : IDynamicContextEntityServicesResolver
    {
        private readonly Dictionary<Type, Type> _entityServices;
        private readonly Dictionary<string, Type> _entityNameService;
        private readonly ILogger<$contextNameSpace$EntityServicesResolver> _logger;
        private static readonly TimeSpan DbContextPoolingTimeout = TimeSpan.FromMinutes(30);
        private static readonly int MaxPoolSize = 1024;

        public $contextNameSpace$EntityServicesResolver(ILogger<$contextNameSpace$EntityServicesResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _entityServices = new Dictionary<Type, Type>();
            _entityNameService = new Dictionary<string, Type>();
            RegisterTypeMappings();
        \}

        private void RegisterTypeMappings()
        {
            _logger.LogDebug("Registering entity type mappings");

            $entityList: {entity|
                _entityServices.Add(typeof(I$entity.Name$Service), typeof($entity.Name$Service));
                _entityNameService.Add("$entity.Name$", typeof(I$entity.Name$Service));
            }$
        \}

        public Dictionary<Type, Type> EntityServices => _entityServices;
        public Dictionary<string, Type> EntityNameService => _entityNameService;
        public string DynamicContextName => "$contextNameSpace$";

        /// <summary>
        /// Configures services for the dynamic context
        /// </summary>
        public void ConfigureServices(IServiceCollection services, DbConnectionType connectionType, string connectionString, ILogger logger)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            logger.LogInformation("Configuring services for context: $contextNameSpace$");

            try
            {
                ConfigureDbContext(services, connectionType, connectionString, logger);
                RegisterRepositories(services, logger);
                RegisterServices(services, logger);

                logger.LogInformation("Successfully configured services for context: $contextNameSpace$");
            \}
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure services for context: $contextNameSpace$");
                throw new DynamicContextException("Failed to configure services", ex);
            \}
        \}

        private static void ConfigureDbContext(IServiceCollection services, DbConnectionType connectionType, string connectionString, ILogger logger)
        {
            logger.LogDebug("Configuring DbContext for connection type: " + connectionType.ToString());

            Action<DbContextOptionsBuilder> configureOptions = options =>
            {
                // Performance and debugging options
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
                options.UseLazyLoadingProxies(false);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                // Configure provider
                switch (connectionType)
                {
                    case DbConnectionType.SqlServer:
                        logger.LogDebug("Using SQL Server provider");
                        options.UseSqlServer(connectionString, sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure();
                            sqlOptions.CommandTimeout(30);
                        \});
                        break;
                    case DbConnectionType.MySql:
                        logger.LogDebug("Using MySQL provider");
                        var serverVersion = new MariaDbServerVersion(new Version(10, 3, 9));
                        options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                        {
                            mySqlOptions.EnableRetryOnFailure();
                            mySqlOptions.CommandTimeout(30);
                        \});
                        break;
                    case DbConnectionType.PgSql:
                        logger.LogDebug("Using PostgreSQL provider");
                        options.UseNpgsql(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.EnableRetryOnFailure();
                            npgsqlOptions.CommandTimeout(30);
                        \});
                        break;
                    default:
                        throw new ArgumentException("Unsupported connection type: " + connectionType.ToString());
                \}
            \};

            // Register pooled factory with custom options
            services.AddPooledDbContextFactory<$contextNameSpace$>(options =>
            {
                configureOptions(options);
                options.EnableServiceProviderCaching(true);
            \}, poolSize: MaxPoolSize);

            // Register scoped context
            services.AddDbContext<$contextNameSpace$>(configureOptions);

            // Register options for dependency injection
            services.AddSingleton(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<$contextNameSpace$>();
                configureOptions(optionsBuilder);
                return optionsBuilder.Options;
            \});
        \}

        private void RegisterRepositories(IServiceCollection services, ILogger logger)
        {
            logger.LogDebug("Registering repositories");

            $entityList: {entity|
                services.AddScoped<I$entity.Name$Repository, $entity.Name$Repository>();
            }$
        \}

        private void RegisterServices(IServiceCollection services, ILogger logger)
        {
            logger.LogDebug("Registering services");

            foreach (var (serviceInterface, serviceImplementation) in _entityServices)
            {
                logger.LogDebug("Registering service: " + serviceInterface.Name + " -> " + serviceImplementation.Name);
                services.AddScoped(serviceInterface, serviceImplementation);
            \}
        \}

        public Type GetEntityServiceType(string entityName)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                _logger.LogWarning("Attempted to get service type with null or empty entity name");
                return null;
            \}

            _logger.LogDebug("Getting service type for entity: {EntityName}", entityName);
            return _entityNameService.TryGetValue(entityName, out Type serviceType) ? serviceType : null;
        \}

        public void RegisterEntityServiceType(string entityName, Type serviceType)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                throw new ArgumentException("Entity name cannot be null or empty", nameof(entityName));
            \}
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            \}

            _logger.LogDebug("Registering service type {ServiceType} for entity: {EntityName}", serviceType.Name, entityName);
            _entityNameService[entityName] = serviceType;
        \}

        public bool HasEntityService(string entityName)
        {
            if (string.IsNullOrEmpty(entityName))
            {
                _logger.LogWarning("Attempted to check service existence with null or empty entity name");
                return false;
            \}

            _logger.LogDebug("Checking service existence for entity: {EntityName}", entityName);
            return _entityNameService.ContainsKey(entityName);
        \}
    \}
\} 