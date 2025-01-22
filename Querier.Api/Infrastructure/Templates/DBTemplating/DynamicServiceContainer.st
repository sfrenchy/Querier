using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Application.Interfaces.Infrastructure;
using $nameSpace$.Repositories;
using $nameSpace$.Services;

namespace $nameSpace$
{
    /// <summary>
    /// Conteneur de services pour le contexte dynamique $contextNameSpace$
    /// </summary>
    public class $contextNameSpace$ServiceContainer : IDynamicContextServiceContainer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _entityServices;
        private readonly Dictionary<string, Type> _entityNameService;
        private readonly Dictionary<Type, Type> _proceduresServices;
        private readonly Dictionary<string, Type> _procedureNameService;
        private readonly ILogger _logger;

        public $contextNameSpace$ServiceContainer(IServiceProvider serviceProvider, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _entityServices = new Dictionary<Type, Type>();
            _entityNameService = new Dictionary<string, Type>();
            _proceduresServices = new Dictionary<Type, Type>();
            _procedureNameService = new Dictionary<string, Type>();
        \}

        public T GetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        \}

        public IEnumerable<T> GetServices<T>() where T : class
        {
            return _serviceProvider.GetServices<T>();
        \}

        public IServiceScope CreateScope()
        {
            return _serviceProvider.CreateScope();
        \}

        public Type GetEntityServiceType(string entityName)
        {
            return _entityNameService.TryGetValue(entityName, out Type serviceType) ? serviceType : null;
        \}

        public void RegisterEntityServiceType(string entityName, Type serviceType)
        {
            _entityNameService[entityName] = serviceType;
        \}

        public bool HasEntityService(string entityName)
        {
            return _entityNameService.ContainsKey(entityName);
        \}

        public Type GetProcedureServiceType(string procedureName)
        {
            return _procedureNameService.TryGetValue(procedureName, out Type serviceType) ? serviceType : null;
        \}

        public void RegisterProcedureServiceType(string procedureName, Type serviceType)
        {
            _procedureNameService[procedureName] = serviceType;
        \}

        public bool HasProcedureService(string procedureName)
        {
            return _procedureNameService.ContainsKey(procedureName);
        \}

        public void ConfigureServices(IServiceCollection services, DbConnectionType connectionType, string connectionString, ILogger logger)
        {
            _logger.LogInformation("Configuration des services pour " + typeof($contextNameSpace$ServiceContainer).Name);

            // Configuration du DbContext
            services.AddDbContextFactory<$contextNameSpace$>(options =>
            {
                switch (connectionType)
                {
                    case DbConnectionType.SqlServer:
                        options.UseSqlServer(connectionString, sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(3);
                            sqlOptions.CommandTimeout(30);
                        \});
                        break;
                    case DbConnectionType.MySql:
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
                        {
                            mySqlOptions.EnableRetryOnFailure(3);
                            mySqlOptions.CommandTimeout(30);
                        \});
                        break;
                    case DbConnectionType.PgSql:
                        options.UseNpgsql(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.EnableRetryOnFailure(3);
                            npgsqlOptions.CommandTimeout(30);
                        \});
                        break;
                    default:
                        throw new ArgumentException("Type de connexion non supporté : " + connectionType.ToString());
                \}
            \});

            // Enregistrement des repositories
            $entityList: {entity|
            services.AddScoped<I$entity.Name$Repository, $entity.Name$Repository>();
            }$

            // Enregistrement des services
            $entityList: {entity|
            services.AddScoped<I$entity.Name$Service, $entity.Name$Service>();
            RegisterEntityServiceType("$entity.Name$", typeof(I$entity.Name$Service));
            }$

            $procedureList: {procedure|
            services.AddScoped<I$procedure.CSName$Service, $procedure.CSName$Service>();
            RegisterProcedureServiceType("$procedure.CSName$", typeof(I$procedure.CSName$Service));
            }$

            _logger.LogInformation("Configuration des services terminée pour " + typeof($contextNameSpace$ServiceContainer).Name);
        \}

        public bool ValidateConfiguration()
        {
            try
            {
                // Vérification de l'accès au DbContext
                using var scope = CreateScope();
                var dbContext = scope.ServiceProvider.GetService<$contextNameSpace$>();
                if (dbContext == null)
                {
                    _logger.LogError("DbContext non configuré");
                    return false;
                \}

                // Vérification des services d'entités
                foreach (var entityName in _entityNameService.Keys)
                {
                    var serviceType = GetEntityServiceType(entityName);
                    if (serviceType == null)
                    {
                        _logger.LogError("Service d'entité non trouvé pour {EntityName}", entityName);
                        return false;
                    \}
                \}

                // Vérification des services de procédures
                foreach (var procedureName in _procedureNameService.Keys)
                {
                    var serviceType = GetProcedureServiceType(procedureName);
                    if (serviceType == null)
                    {
                        _logger.LogError("Service de procédure non trouvé pour {ProcedureName}", procedureName);
                        return false;
                    \}
                \}

                _logger.LogInformation("Validation de la configuration réussie");
                return true;
            \}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de la configuration");
                return false;
            \}
        \}
    \}
\}