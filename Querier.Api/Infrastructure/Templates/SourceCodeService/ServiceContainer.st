using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Application.Interfaces.Infrastructure;
using $model.RootNamespace$.Repositories;
$if(model.HasProcedures)$
using $model.RootNamespace$.Repositories.Procedure;
using $model.RootNamespace$.Services.Procedure;
$endif$
using $model.RootNamespace$.Services;
using $model.RootNamespace$.Contexts;

namespace $model.RootNamespace$
{
    /// <summary>
    /// Conteneur de services pour le contexte dynamique $model.RootNamespace$
    /// </summary>
    public class $model.RootNamespace$DbContextServiceContainer : IDynamicContextServiceContainer
    {
        private readonly ILogger _logger;
        private readonly IServiceCollection _services;
        private IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _entityServices;
        private readonly Dictionary<string, Type> _entityNameService;
        private readonly Dictionary<Type, Type> _proceduresServices;
        private readonly Dictionary<string, Type> _procedureNameService;

        public $model.RootNamespace$DbContextServiceContainer(IServiceCollection services, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _entityServices = new Dictionary<Type, Type>();
            _entityNameService = new Dictionary<string, Type>();
            _proceduresServices = new Dictionary<Type, Type>();
            _procedureNameService = new Dictionary<string, Type>();
        }

        public IServiceProvider ServiceProvider => _serviceProvider;

        public T GetService<T>() where T : class
        {
            EnsureServiceProvider();
            return _serviceProvider.GetService<T>();
        }

        public IEnumerable<T> GetServices<T>() where T : class
        {
            EnsureServiceProvider();
            return _serviceProvider.GetServices<T>();
        }

        public IServiceScope CreateScope()
        {
            EnsureServiceProvider();
            return _serviceProvider.CreateScope();
        }

        private void EnsureServiceProvider()
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("Service provider not initialized. Call ConfigureServices first.");
            }
        }

        public Type GetEntityServiceType(string entityName)
        {
            return _entityNameService.TryGetValue(entityName, out Type serviceType) ? serviceType : null;
        }

        public void RegisterEntityServiceType(string entityName, Type serviceType)
        {
            _entityNameService[entityName] = serviceType;
        }

        public bool HasEntityService(string entityName)
        {
            return _entityNameService.ContainsKey(entityName);
        }

        public Type GetProcedureServiceType(string procedureName)
        {
            return _procedureNameService.TryGetValue(procedureName, out Type serviceType) ? serviceType : null;
        }

        public void RegisterProcedureServiceType(string procedureName, Type serviceType)
        {
            _procedureNameService[procedureName] = serviceType;
        }

        public bool HasProcedureService(string procedureName)
        {
            return _procedureNameService.ContainsKey(procedureName);
        }

        public void ConfigureServices(IServiceCollection services, DbConnectionType connectionType, string connectionString, ILogger logger)
        {
            _logger.LogInformation("Configuration des services pour " + nameof($model.RootNamespace$DbContextServiceContainer));

            // Configure DbContext
            services.AddPooledDbContextFactory<$model.RootNamespace$DbContext>(options =>
            {
                switch (connectionType)
                {
                    case DbConnectionType.SqlServer:
                        options.UseSqlServer(connectionString, sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(3);
                            sqlOptions.CommandTimeout(30);
                        });
                        break;
                    case DbConnectionType.MySql:
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
                        {
                            mySqlOptions.EnableRetryOnFailure(3);
                            mySqlOptions.CommandTimeout(30);
                        });
                        break;
                    case DbConnectionType.PgSql:
                        options.UseNpgsql(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.EnableRetryOnFailure(3);
                            npgsqlOptions.CommandTimeout(30);
                        });
                        break;
                    case DbConnectionType.SQLite:
                        options.UseSqlite(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.CommandTimeout(30);
                        });
                        break;
                    default:
                        throw new ArgumentException("Type de connexion non supporté : " + connectionType.ToString());
                }
            });
            
            // Configure ReadOnlyContext
            services.AddPooledDbContextFactory<$model.RootNamespace$DbContextReadOnly>(options =>
            {
                switch (connectionType)
                {
                    case DbConnectionType.SqlServer:
                        options.UseSqlServer(connectionString, sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(3);
                            sqlOptions.CommandTimeout(30);
                        });
                        break;
                    case DbConnectionType.MySql:
                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySqlOptions =>
                        {
                            mySqlOptions.EnableRetryOnFailure(3);
                            mySqlOptions.CommandTimeout(30);
                        });
                        break;
                    case DbConnectionType.PgSql:
                        options.UseNpgsql(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.EnableRetryOnFailure(3);
                            npgsqlOptions.CommandTimeout(30);
                        });
                        break;
                    case DbConnectionType.SQLite:
                        options.UseSqlite(connectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.CommandTimeout(30);
                        });
                        break;
                    default:
                        throw new ArgumentException("Type de connexion non supporté : " + connectionType.ToString());
                }
            });

            // Enregistrement des repositories
            $model.EntityRepositories: {entity|
                services.AddScoped<I$entity$, $entity$>();
            }$

            $model.ProcedureRepositories: {procedure|
                services.AddScoped<I$procedure$, $procedure$>();
            }$
            
            // Enregistrement des services
            $model.EntityServices: {entity|
                services.AddScoped<I$entity$, $entity$>();
                RegisterEntityServiceType("$entity$", typeof(I$entity$));
            }$

            $model.ProcedureServices: {procedure|
                services.AddScoped<I$procedure$, $procedure$>();
                RegisterProcedureServiceType("$procedure$", typeof(I$procedure$));
            }$

            _logger.LogInformation("Configuration des services terminée pour " + nameof($model.RootNamespace$DbContextServiceContainer));

            // Construction du service provider après la configuration complète
            _serviceProvider = services.BuildServiceProvider();
        }

        public bool ValidateConfiguration()
        {
            try
            {
                _logger.LogInformation("Validating service container configuration");
                
                // Vérifier que les services essentiels sont disponibles
                var requiredServices = new[]
                {
                    typeof(ILogger),
                    typeof(IServiceProvider),
                    // Ajouter d'autres services requis ici
                };

                foreach (var serviceType in requiredServices)
                {
                    var service = _serviceProvider.GetService(serviceType);
                    if (service == null)
                    {
                        _logger.LogError("Required service " + serviceType.Name + " is not registered");
                        return false;
                    }
                }

                // Vérification de l'accès au DbContext
                using var scope = CreateScope();
                var dbContext = scope.ServiceProvider.GetService<$model.RootNamespace$DbContext>();
                if (dbContext == null)
                {
                    _logger.LogError("DbContext non configuré");
                    return false;
                }

                // Vérification des services d'entités
                foreach (var entityName in _entityNameService.Keys)
                {
                    var serviceType = GetEntityServiceType(entityName);
                    if (serviceType == null)
                    {
                        _logger.LogError("Service d'entité non trouvé pour " + entityName);
                        return false;
                    }
                }

                // Vérification des services de procédures
                foreach (var procedureName in _procedureNameService.Keys)
                {
                    var serviceType = GetProcedureServiceType(procedureName);
                    if (serviceType == null)
                    {
                        _logger.LogError("Service de procédure non trouvé pour " + procedureName);
                        return false;
                    }
                }

                _logger.LogInformation("Service container configuration is valid");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating service container configuration");
                return false;
            }
        }
    }
}