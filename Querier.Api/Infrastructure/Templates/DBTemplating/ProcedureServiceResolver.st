using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using $nameSpace$.Exceptions;
using $nameSpace$.Repositories;
using $nameSpace$.Services;
using Querier.Api.Application.Interfaces.Infrastructure;

namespace $nameSpace$.Infrastructure
{
    /// <summary>
    /// Resolver for dynamic stored procedure services in the $contextNameSpace$ context
    /// </summary>
    public class $contextNameSpace$ProceduresServicesResolver : IDynamicContextProceduresServicesResolver
    {
        private readonly Dictionary<Type, Type> _proceduresServices;
        private readonly Dictionary<string, Type> _procedureNameService;
        private readonly ILogger<$contextNameSpace$ProceduresServicesResolver> _logger;

        public $contextNameSpace$ProceduresServicesResolver(ILogger<$contextNameSpace$ProceduresServicesResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _proceduresServices = new Dictionary<Type, Type>();
            _procedureNameService = new Dictionary<string, Type>();
            RegisterTypeMappings();
        \}

        private void RegisterTypeMappings()
        {
            _logger.LogDebug("Registering procedure type mappings");

            $procedureList: {procedure|
                _proceduresServices.Add(typeof(I$procedure.CSName$Service), typeof($procedure.CSName$Service));
                _procedureNameService.Add("$procedure.CSName$", typeof(I$procedure.CSName$Service));
            }$
        \}

        public Type GetProcedureServiceType(string procedureName)
        {
            if (string.IsNullOrEmpty(procedureName))
            {
                _logger.LogWarning("Attempted to get service type with null or empty procedure name");
                return null;
            \}

            _logger.LogDebug("Getting service type for procedure: " + procedureName);
            return _procedureNameService.TryGetValue(procedureName, out Type serviceType) ? serviceType : null;
        \}

        public void RegisterProcedureServiceType(string procedureName, Type serviceType)
        {
            if (string.IsNullOrEmpty(procedureName))
            {
                throw new ArgumentException("Procedure name cannot be null or empty", nameof(procedureName));
            \}
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            \}

            _logger.LogDebug("Registering service type " + serviceType.Name + " for procedure: " + procedureName);
            _procedureNameService[procedureName] = serviceType;
        \}

        public bool HasProcedureService(string procedureName)
        {
            if (string.IsNullOrEmpty(procedureName))
            {
                _logger.LogWarning("Attempted to check service existence with null or empty procedure name");
                return false;
            \}

            _logger.LogDebug("Checking service existence for procedure: " + procedureName);
            return _procedureNameService.ContainsKey(procedureName);
        \}

        public Dictionary<Type, Type> ProcedureServices => _proceduresServices;
        public Dictionary<string, Type> ProcedureNameService => _procedureNameService;
        public string DynamicContextName => "$contextNameSpace$";

        /// <summary>
        /// Configures services for the dynamic context
        /// </summary>
        public void ConfigureServices(IServiceCollection services, ILogger logger)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            logger.LogInformation("Configuring procedure services for context: $contextNameSpace$");

            try
            {
                RegisterRepositories(services, logger);
                RegisterServices(services, logger);

                logger.LogInformation("Successfully configured procedure services for context: $contextNameSpace$");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure procedure services for context: $contextNameSpace$");
                throw new DynamicContextException("Failed to configure procedure services", ex);
            }
        \}

        private void RegisterRepositories(IServiceCollection services, ILogger logger)
        {
            logger.LogDebug("Registering procedure repositories");

            $procedureList: {procedure|
                services.AddScoped<I$procedure.CSName$Repository, $procedure.CSName$Repository>();
            }$
        \}

        private void RegisterServices(IServiceCollection services, ILogger logger)
        {
            logger.LogDebug("Registering procedure services");

            foreach (var (serviceInterface, serviceImplementation) in _proceduresServices)
            {
                logger.LogDebug("Registering service: {ServiceInterface} -> {ServiceImplementation}", 
                    serviceInterface.Name, serviceImplementation.Name);
                services.AddScoped(serviceInterface, serviceImplementation);
            \}
        \}
    \}
\}