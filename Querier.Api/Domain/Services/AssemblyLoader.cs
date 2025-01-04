using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Entities.QDBConnection;
using System.Security.Cryptography;
using System.Security;
using System.Runtime.Loader;

namespace Querier.Api.Domain.Services
{
    public static class AssemblyLoader
    {
        private static bool VerifyAssemblyIntegrity(string assemblyPath, string expectedHash, ILogger logger)
        {
            try
            {
                var assemblyBytes = File.ReadAllBytes(assemblyPath);
                using (var sha256 = SHA256.Create())
                {
                    var actualHash = Convert.ToBase64String(sha256.ComputeHash(assemblyBytes));
                    return expectedHash == actualHash;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error verifying assembly integrity for {assemblyPath}");
                return false;
            }
        }

        public static async Task LoadAssemblyFromQDBConnection(
            QDBConnection connection,
            IServiceProvider serviceProvider,
            ApplicationPartManager partManager,
            ILogger logger)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => Path.GetFileName(a.Location))
                .ToList();

            var assemblyName = $"{connection.Name}.DynamicContext.dll";
            if (!loadedAssemblies.Contains(assemblyName))
            {
                try
                {
                    // Vérifier l'intégrité de l'assembly
                    if (string.IsNullOrEmpty(connection.AssemblyHash) || 
                        connection.AssemblyDll == null ||
                        ComputeHash(connection.AssemblyDll) != connection.AssemblyHash)
                    {
                        logger.LogError($"Assembly integrity verification failed for {assemblyName}");
                        throw new SecurityException($"Invalid assembly integrity for {assemblyName}");
                    }

                    // Charger l'assembly depuis la mémoire
                    var assemblyLoadContext = new AssemblyLoadContext(connection.Name, false);
                    var assembly = assemblyLoadContext.LoadFromStream(new MemoryStream(connection.AssemblyDll));

                        // Load procedure services
                        var procedureServicesResolverType = assembly.GetTypes()
                            .FirstOrDefault(t => typeof(IDynamicContextProceduresServicesResolver).IsAssignableFrom(t));

                        if (procedureServicesResolverType != null)
                        {
                            var resolver = (IDynamicContextProceduresServicesResolver)Activator.CreateInstance(procedureServicesResolverType);

                            // Ajouter au DynamicContextList
                            var dynamicContextList = DynamicContextList.Instance;

                            resolver.ConfigureServices((IServiceCollection)serviceProvider.GetService(typeof(IServiceCollection)), connection.ConnectionString);
                            var dynamicContextListService = serviceProvider.GetRequiredService<IDynamicContextList>();
                            logger.LogInformation($"Adding DynamicContext {connection.Name} for procedures");
                            dynamicContextListService.DynamicContexts.Add(connection.Name, resolver);

                            foreach (KeyValuePair<Type, Type> service in resolver.ProceduresServices)
                            {
                                logger.LogInformation($"Registering procedure service {service.Key}");
                                serviceProvider.GetRequiredService<IServiceCollection>().AddSingleton(service.Key, service.Value);
                            }
                        }

                        // Load entity services
                        var entityServicesResolverType = assembly.GetTypes()
                            .FirstOrDefault(t => typeof(IDynamicContextEntityServicesResolver).IsAssignableFrom(t));

                        if (entityServicesResolverType != null)
                        {
                            var resolver = (IDynamicContextEntityServicesResolver)Activator.CreateInstance(entityServicesResolverType);

                            resolver.ConfigureServices((IServiceCollection)serviceProvider.GetService(typeof(IServiceCollection)), connection.ConnectionString);
                            logger.LogInformation($"Adding DynamicContext {connection.Name} for entities");

                            foreach (KeyValuePair<Type, Type> service in resolver.EntityServices)
                            {
                                logger.LogInformation($"Registering entity service {service.Key}");
                                serviceProvider.GetRequiredService<IServiceCollection>().AddScoped(service.Key, service.Value);
                            }
                        }

                        // Ajouter les contrôleurs dynamiquement
                        partManager.ApplicationParts.Add(new AssemblyPart(assembly));
                        var feature = new ControllerFeature();
                        partManager.PopulateFeature(feature);

                        // Log des contrôleurs trouvés
                        foreach (var controller in feature.Controllers)
                        {
                            logger.LogInformation($"Found controller: {controller.FullName}");
                            foreach (var method in controller.GetMethods())
                            {
                                var attributes = method.GetCustomAttributes(typeof(HttpGetAttribute), true)
                                    .Concat(method.GetCustomAttributes(typeof(HttpPostAttribute), true))
                                    .Concat(method.GetCustomAttributes(typeof(HttpPutAttribute), true))
                                    .Concat(method.GetCustomAttributes(typeof(HttpDeleteAttribute), true));
                                if (attributes.Any())
                                {
                                    logger.LogInformation($"  - Route: {method.Name}");
                                }
                            }
                        }

                    logger.LogInformation($"Successfully loaded assembly {assemblyName} for context {connection.Name}");
                    }
                    catch (Exception ex)
                    {
                    logger.LogError(ex, $"Error loading assembly {assemblyName}");
                }
            }
        }

        public static void RegenerateSwagger(ISwaggerProvider swaggerProvider, ILogger logger)
        {
            try
            {
                // Forcer un rechargement complet du document Swagger
                var apiDescriptionGroups = swaggerProvider.GetType()
                    .GetField("_apiDescriptionGroupCollectionProvider",
                        BindingFlags.NonPublic | BindingFlags.Instance)?
                    .GetValue(swaggerProvider) as IApiDescriptionGroupCollectionProvider;

                if (apiDescriptionGroups != null)
                {
                    // Déclencher une actualisation des descriptions d'API
                    var apiDescriptions = apiDescriptionGroups.ApiDescriptionGroups;
                }

                var swagger = swaggerProvider.GetSwagger("v1", null, "/");
                logger.LogInformation($"Swagger regenerated with {swagger.Paths.Count} paths");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error regenerating Swagger");
            }
        }

        private static string ComputeHash(byte[] assemblyBytes)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(assemblyBytes);
                return Convert.ToBase64String(hash);
            }
        }
    }
}