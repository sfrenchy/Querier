using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Infrastructure.Services
{
    public class AssemblyManagerService : IAssemblyManagerService
    {
        private readonly ILogger<AssemblyManagerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceCollection _services;
        private readonly ApplicationPartManager _partManager;
        private readonly ConcurrentDictionary<string, AssemblyLoadContext> _loadContexts;
        private readonly ConcurrentDictionary<string, IDynamicContextServiceContainer> _serviceContainers;
        private readonly ConcurrentDictionary<string, ServiceCollection> _assemblyServices;
        private readonly ConcurrentDictionary<string, IServiceProvider> _assemblyServiceProviders;
        private readonly ConcurrentDictionary<string, string> _normalizedNameCache;
        public AssemblyManagerService(
            ILogger<AssemblyManagerService> logger,
            IServiceProvider serviceProvider,
            IServiceCollection services,
            ApplicationPartManager partManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _partManager = partManager ?? throw new ArgumentNullException(nameof(partManager));
            _loadContexts = new ConcurrentDictionary<string, AssemblyLoadContext>();
            _serviceContainers = new ConcurrentDictionary<string, IDynamicContextServiceContainer>();
            _assemblyServices = new ConcurrentDictionary<string, ServiceCollection>();
            _assemblyServiceProviders = new ConcurrentDictionary<string, IServiceProvider>();
            _normalizedNameCache = new ConcurrentDictionary<string, string>();
        }

        public string GetContextNormalizedAssemblyName(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                throw new ArgumentException("Assembly name cannot be null or empty", nameof(assemblyName));

            return _normalizedNameCache.GetOrAdd(assemblyName, name =>
            {
                // Enlever l'extension .dll si présente
                if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - 4);
                }

                // Si le nom contient _DataContext, le retirer
                if (name.EndsWith("_DataContext", StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - 12);
                }

                return name;
            });
        }

        private IServiceCollection CreateServiceCollectionForAssembly(string name)
        {
            var normalizedName = GetContextNormalizedAssemblyName(name);
            var services = new ServiceCollection();
            
            // Ajouter ServiceCollection comme singleton pour IServiceCollection
            services.Add(new ServiceDescriptor(typeof(IServiceCollection), services));

            // Ajouter les services de logging
            var loggerFactory = _services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            services.AddSingleton(loggerFactory);
            services.AddSingleton<ILogger>(sp => loggerFactory.CreateLogger(normalizedName));
            services.AddLogging();

            // Ajouter les services de cache
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            return services;
        }

        private async Task<IDynamicContextServiceContainer> ConfigureServicesAndCreateContainer(
            string name,
            Assembly assembly,
            DbConnectionType connectionType,
            string connectionString)
        {
            _logger.LogInformation("Configuring services for assembly: {Name}", name);

            try
            {
                // Créer la collection de services pour l'assembly
                var services = (ServiceCollection)CreateServiceCollectionForAssembly(name);

                // Créer une instance du conteneur de services
                var containerType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IDynamicContextServiceContainer).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                if (containerType == null)
                {
                    throw new InvalidOperationException($"No service container implementation found in assembly {name}");
                }

                // Ajouter l'assembly part au gestionnaire
                _partManager.ApplicationParts.Add(new AssemblyPart(assembly));

                // Configurer les services avant de construire le provider
                var container = (IDynamicContextServiceContainer)Activator.CreateInstance(
                    containerType, 
                    services, 
                    _logger);

                container.ConfigureServices(services, connectionType, connectionString, _logger);

                // Construire le provider une seule fois après la configuration
                var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions 
                { 
                    ValidateScopes = true,
                    ValidateOnBuild = true
                });

                // Stocker les services et le provider
                _assemblyServices.TryAdd(name, services);
                _assemblyServiceProviders.TryAdd(name, serviceProvider);

                _logger.LogInformation("Services configured successfully for assembly: {Name}", name);
                return container;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring services for assembly: {Name}", name);
                throw;
            }
        }

        public async Task<IDynamicContextServiceContainer> LoadAssemblyAsync(DBConnectionDto connection)
        {
            try
            {
                if (connection == null)
                {
                    _logger.LogError("Connection parameter is null");
                    throw new ArgumentNullException(nameof(connection));
                }

                var normalizedName = GetContextNormalizedAssemblyName(connection.Name);
                _logger.LogInformation("Loading assembly for connection: {ConnectionName}", normalizedName);

                // Vérifier si l'assembly est déjà chargée
                if (IsAssemblyLoaded(normalizedName))
                {
                    _logger.LogInformation("Assembly already loaded for {ConnectionName}", normalizedName);
                    return GetServiceContainer(normalizedName);
                }

                // Créer un nouveau contexte de chargement pour l'assembly
                var loadContext = new AssemblyLoadContext(normalizedName, isCollectible: true);
                _loadContexts.TryAdd(normalizedName, loadContext);

                // Charger l'assembly
                Assembly assembly;
                using (var scope = _serviceProvider.CreateScope())
                {
                    IDbConnectionRepository dbConnectionRepository = (IDbConnectionRepository) scope.ServiceProvider.GetRequiredService(typeof(IDbConnectionRepository));

                    
                    using (var assemblyStream = new MemoryStream(await dbConnectionRepository.GetDLLStreamAsync(connection.Id)))
                    using (var pdbStream = new MemoryStream(await dbConnectionRepository.GetPDBStreamAsync(connection.Id)))
                    {
                        assembly = loadContext.LoadFromStream(assemblyStream, pdbStream);
                    }
                }



                string connectionString = string.Join(';', connection.Parameters.Where(p => !p.IsEncrypted).Select(p => p.Key + "=" + p.Value));
                foreach (var cryptedParameter in connection.Parameters.Where(p => p.IsEncrypted))
                {
                    using var scope = _serviceProvider.CreateScope();
                    var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
                    string uncryptedParameterValue = await encryptionService.DecryptAsync(cryptedParameter.Value);
                    connectionString += $";{cryptedParameter.Key}={uncryptedParameterValue}";
                }
                // Configurer les services et créer le conteneur
                var container = await ConfigureServicesAndCreateContainer(
                    normalizedName,
                    assembly,
                    connection.ConnectionType,
                    connectionString);

                _serviceContainers.TryAdd(normalizedName, container);
                await RegenerateSwaggerAsync();
                return container;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load assembly for connection: {ConnectionName}", connection.Name);
                throw;
            }
        }

        public async Task<IDynamicContextServiceContainer> LoadDbConnectionAssemblyAsync(
            string name,
            DbConnectionType connectionType,
            string connectionString,
            byte[] assemblyBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Name cannot be null or empty", nameof(name));
                if (assemblyBytes == null)
                    throw new ArgumentNullException(nameof(assemblyBytes));

                var normalizedName = GetContextNormalizedAssemblyName(name);
                _logger.LogInformation("Loading assembly from bytes for {Name}", normalizedName);

                if (IsAssemblyLoaded(normalizedName))
                {
                    _logger.LogInformation("Assembly already loaded for {Name}", normalizedName);
                    return GetServiceContainer(normalizedName);
                }

                var loadContext = new AssemblyLoadContext(normalizedName, isCollectible: true);
                _loadContexts.TryAdd(normalizedName, loadContext);

                var assembly = loadContext.LoadFromStream(new MemoryStream(assemblyBytes));
                var container = await ConfigureServicesAndCreateContainer(normalizedName, assembly, connectionType, connectionString);
                
                _serviceContainers.TryAdd(normalizedName, container);
                await RegenerateSwaggerAsync();

                return container;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assembly from bytes for {Name}", name);
                throw;
            }
        }

        public async Task LoadQueryAssemblyAsync(string assemblyLoaderName, byte[] assemblyByte)
        {
            if (!_loadContexts.ContainsKey(assemblyLoaderName))
                throw new Exception($"No assembly loader with name {assemblyLoaderName}");

            _loadContexts[assemblyLoaderName].LoadFromStream(new MemoryStream(assemblyByte));
        }

        public IEnumerable<Assembly> GetAssemblies(string assemblyLoaderName)
        {
            return _loadContexts.TryGetValue(assemblyLoaderName, out var context) ? context.Assemblies : [];
        }
        
        
        public async Task UnloadAssemblyAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            _logger.LogInformation("Unloading assembly: {Name}", name);

            // Supprimer le conteneur de services
            _serviceContainers.TryRemove(name, out _);

            // Nettoyer les services de l'assembly
            if (_assemblyServiceProviders.TryRemove(name, out var provider))
            {
                if (provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _assemblyServices.TryRemove(name, out _);

            // Décharger le contexte de l'assembly
            if (_loadContexts.TryRemove(name, out var loadContext))
            {
                loadContext.Unload();
            }

            await RegenerateSwaggerAsync();
        }

        public IDynamicContextServiceContainer GetServiceContainer(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            _serviceContainers.TryGetValue(name, out var container);
            return container;
        }

        public bool IsAssemblyLoaded(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            return _loadContexts.ContainsKey(name);
        }

        private async Task RegenerateSwaggerAsync()
        {
            try
            {
                var scope = _serviceProvider.CreateScope();
                var actionDescriptorCollectionProvider = scope.ServiceProvider.GetService<IActionDescriptorCollectionProvider>();
                if (actionDescriptorCollectionProvider != null)
                {
                    // Forcer le rechargement des contrôleurs
                    var actionDescriptorField = actionDescriptorCollectionProvider.GetType()
                        .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (actionDescriptorField != null)
                    {
                        actionDescriptorField.SetValue(actionDescriptorCollectionProvider, null);
                    }

                    // Déclencher la découverte des contrôleurs
                    var actions = actionDescriptorCollectionProvider.ActionDescriptors;
                    _logger.LogInformation("Controller actions reloaded with {Count} actions", actions.Items.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating Swagger documentation");
                throw;
            }
        }

        private static string ComputeHash(byte[] assemblyBytes)
        {
            if (assemblyBytes == null)
                throw new ArgumentNullException(nameof(assemblyBytes));

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(assemblyBytes);
            return Convert.ToBase64String(hash);
        }
    }
}   