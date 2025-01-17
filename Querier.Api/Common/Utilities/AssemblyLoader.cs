using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Entities.DBConnection;
using Swashbuckle.AspNetCore.Swagger;

namespace Querier.Api.Common.Utilities
{
    public static class AssemblyLoader
    {
        private static bool VerifyAssemblyIntegrity(string assemblyPath, string expectedHash, ILogger logger)
        {
            if (string.IsNullOrEmpty(assemblyPath))
            {
                logger.LogError("Assembly path is null or empty");
                throw new ArgumentException("Assembly path cannot be null or empty", nameof(assemblyPath));
            }

            if (string.IsNullOrEmpty(expectedHash))
            {
                logger.LogError("Expected hash is null or empty");
                throw new ArgumentException("Expected hash cannot be null or empty", nameof(expectedHash));
            }

            try
            {
                logger.LogDebug("Verifying integrity of assembly: {AssemblyPath}", assemblyPath);
                var assemblyBytes = File.ReadAllBytes(assemblyPath);
                using var sha256 = SHA256.Create();
                var actualHash = Convert.ToBase64String(sha256.ComputeHash(assemblyBytes));
                var isValid = expectedHash == actualHash;
                    
                if (!isValid)
                {
                    logger.LogWarning("Assembly integrity check failed. Expected hash: {ExpectedHash}, Actual hash: {ActualHash}",
                        expectedHash, actualHash);
                }
                else
                {
                    logger.LogDebug("Assembly integrity verified successfully");
                }
                    
                return isValid;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error verifying assembly integrity for {AssemblyPath}", assemblyPath);
                return false;
            }
        }

        public static void LoadAssemblyFromDbConnection(
            DBConnection connection,
            IServiceProvider serviceProvider,
            ApplicationPartManager partManager,
            ILogger logger)
        {
            try
            {
                if (connection == null)
                {
                    logger.LogError("Connection parameter is null");
                    throw new ArgumentNullException(nameof(connection));
                }

                if (serviceProvider == null)
                {
                    logger.LogError("ServiceProvider parameter is null");
                    throw new ArgumentNullException(nameof(serviceProvider));
                }

                if (partManager == null)
                {
                    logger.LogError("PartManager parameter is null");
                    throw new ArgumentNullException(nameof(partManager));
                }

                logger.LogInformation("Loading assembly for connection: {ConnectionName}", connection.Name);

                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => Path.GetFileName(a.Location))
                    .ToList();

                var assemblyName = $"{connection.Name}.DynamicContext.dll";
                if (!loadedAssemblies.Contains(assemblyName))
                {
                    logger.LogDebug("Assembly {AssemblyName} not yet loaded", assemblyName);

                    // Vérifier l'intégrité de l'assembly
                    if (string.IsNullOrEmpty(connection.AssemblyHash) || 
                        connection.AssemblyDll == null ||
                        ComputeHash(connection.AssemblyDll) != connection.AssemblyHash)
                    {
                        logger.LogError("Assembly integrity verification failed for {AssemblyName}", assemblyName);
                        throw new SecurityException($"Invalid assembly integrity for {assemblyName}");
                    }

                    try
                    {
                        // Charger l'assembly depuis la mémoire
                        logger.LogDebug("Creating assembly load context for {AssemblyName}", assemblyName);
                        var assemblyLoadContext = new AssemblyLoadContext(connection.Name);
                        var assembly = assemblyLoadContext.LoadFromStream(new MemoryStream(connection.AssemblyDll));
                        logger.LogInformation("Successfully loaded assembly {AssemblyName}", assemblyName);

                        LoadProcedureServiceAndEntityServicesAndAddMVCParts(
                            assembly,
                            assemblyName,
                            connection.Name,
                            connection.ConnectionString,
                            serviceProvider,
                            partManager,
                            logger
                            );
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error loading assembly {AssemblyName}", assemblyName);
                        throw;
                    }
                }
                else
                {
                    logger.LogInformation("Assembly {AssemblyName} already loaded", assemblyName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LoadAssemblyFromQDBConnection");
                throw;
            }
        }

        public static void LoadProcedureServiceAndEntityServicesAndAddMVCParts(
            Assembly assembly, 
            string assemblyName,
            string connectionName,
            string connectionString,
            IServiceProvider serviceProvider,
            ApplicationPartManager partManager,
            ILogger logger)
        {
            // Load procedure services
            var procedureServicesResolverType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IDynamicContextProceduresServicesResolver).IsAssignableFrom(t));

            if (procedureServicesResolverType != null)
            {
                logger.LogDebug("Found procedure services resolver type");
                var resolver = (IDynamicContextProceduresServicesResolver)Activator.CreateInstance(procedureServicesResolverType);

                if (resolver != null)
                {
                    resolver.ConfigureServices(
                        (IServiceCollection)serviceProvider.GetService(typeof(IServiceCollection)),
                        connectionString);
                    var dynamicContextListService =
                        serviceProvider.GetRequiredService<IDynamicContextList>();
                    logger.LogInformation("Adding DynamicContext {Name} for procedures", connectionName);
                    dynamicContextListService.DynamicContexts.Add(connectionName, resolver);

                    foreach (KeyValuePair<Type, Type> service in resolver.ProceduresServices)
                    {
                        logger.LogInformation("Registering procedure service {ServiceType}", service.Key);
                        serviceProvider.GetRequiredService<IServiceCollection>()
                            .AddSingleton(service.Key, service.Value);
                    }
                }
            }
            // Load entity services
            var entityServicesResolverType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IDynamicContextEntityServicesResolver).IsAssignableFrom(t));

            if (entityServicesResolverType != null)
            {
                logger.LogDebug("Found entity services resolver type");
                var resolver = (IDynamicContextEntityServicesResolver)Activator.CreateInstance(entityServicesResolverType);

                if (resolver != null)
                {
                    resolver.ConfigureServices(
                        (IServiceCollection)serviceProvider.GetService(typeof(IServiceCollection)),
                        connectionString);
                    logger.LogInformation("Adding DynamicContext {Name} for entities", connectionName);

                    foreach (KeyValuePair<Type, Type> service in resolver.EntityServices)
                    {
                        logger.LogInformation("Registering entity service {ServiceType}", service.Key);
                        serviceProvider.GetRequiredService<IServiceCollection>()
                            .AddScoped(service.Key, service.Value);
                    }
                }
            }
            // Ajouter les contrôleurs dynamiquement
            logger.LogDebug("Adding assembly part to part manager");
            partManager.ApplicationParts.Add(new AssemblyPart(assembly));
            var feature = new ControllerFeature();
            partManager.PopulateFeature(feature);

            // Log des contrôleurs trouvés
            foreach (var controller in feature.Controllers)
            {
                logger.LogInformation("Found controller: {ControllerName}", controller.FullName);
                foreach (var method in controller.GetMethods())
                {
                    var attributes = method.GetCustomAttributes(typeof(HttpGetAttribute), true)
                        .Concat(method.GetCustomAttributes(typeof(HttpPostAttribute), true))
                        .Concat(method.GetCustomAttributes(typeof(HttpPutAttribute), true))
                        .Concat(method.GetCustomAttributes(typeof(HttpDeleteAttribute), true));
                    if (attributes.Any())
                    {
                        logger.LogDebug("Route found: {MethodName}", method.Name);
                    }
                }
            }

            logger.LogInformation("Successfully loaded assembly {AssemblyName} for context {ConnectionName}",
                assemblyName, connectionName);
        }

        public static void LoadAssemblyFromByteArray(
            string connectionName,
            string connectionString,
            byte[] assemblyBytes,
            IServiceProvider serviceProvider,
            ApplicationPartManager partManager,
            ILogger logger)
        {
            try
            {
                if (serviceProvider == null)
                {
                    logger.LogError("ServiceProvider parameter is null");
                    throw new ArgumentNullException(nameof(serviceProvider));
                }

                if (partManager == null)
                {
                    logger.LogError("PartManager parameter is null");
                    throw new ArgumentNullException(nameof(partManager));
                }

                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => Path.GetFileName(a.Location))
                    .ToList();

                var assemblyName = $"{connectionName}.DynamicContext.dll";
                if (!loadedAssemblies.Contains(assemblyName))
                {
                    logger.LogDebug("Assembly {AssemblyName} not yet loaded", assemblyName);

                    try
                    {
                        logger.LogDebug("Creating assembly load context for {AssemblyName}", assemblyName);
                        var assemblyLoadContext = new AssemblyLoadContext(connectionName);
                        var assembly = assemblyLoadContext.LoadFromStream(new MemoryStream(assemblyBytes));
                        logger.LogInformation("Successfully loaded assembly {AssemblyName}", assemblyName);
                        LoadProcedureServiceAndEntityServicesAndAddMVCParts(assembly, assemblyName, connectionName, connectionString, serviceProvider, partManager, logger);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error loading assembly {AssemblyName}", assemblyName);
                        throw;
                    }
                }
                else
                {
                    logger.LogInformation("Assembly {AssemblyName} already loaded", assemblyName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in LoadAssemblyFromQDBConnection");
                throw;
            }
        }
        
        public static void RegenerateSwagger(ISwaggerProvider swaggerProvider, ILogger logger)
        {
            try
            {
                if (swaggerProvider == null)
                {
                    logger.LogError("SwaggerProvider parameter is null");
                    throw new ArgumentNullException(nameof(swaggerProvider));
                }

                var scope = ServiceActivator.GetScope();
                if (scope == null)
                {
                    logger.LogInformation("Service scope not available yet, skipping Swagger regeneration");
                    return;
                }

                var actionDescriptorCollectionProvider = scope.ServiceProvider.GetRequiredService<IActionDescriptorCollectionProvider>();
                if (actionDescriptorCollectionProvider == null)
                {
                    logger.LogInformation("ActionDescriptorCollectionProvider not available, skipping Swagger regeneration");
                    return;
                }
                
                // Forcer le rechargement des contrôleurs
                logger.LogDebug("Forcing controller reload");
                var actionDescriptorField = actionDescriptorCollectionProvider.GetType()
                    .GetField("_collection", BindingFlags.NonPublic | BindingFlags.Instance);
                if (actionDescriptorField != null)
                {
                    actionDescriptorField.SetValue(actionDescriptorCollectionProvider, null);
                }

                // Déclencher la découverte des contrôleurs
                var actions = actionDescriptorCollectionProvider.ActionDescriptors;
                logger.LogInformation("Controller actions reloaded with {Count} actions", actions.Items.Count);

                // Régénérer le document Swagger
                logger.LogDebug("Regenerating Swagger document");
                var swagger = swaggerProvider.GetSwagger("v1", null, "/");
                logger.LogInformation("Swagger regenerated with {Count} paths", swagger.Paths.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error regenerating Swagger");
                throw;
            }
        }

        private static string ComputeHash(byte[] assemblyBytes)
        {
            if (assemblyBytes == null)
            {
                throw new ArgumentNullException(nameof(assemblyBytes));
            }

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(assemblyBytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}