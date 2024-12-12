using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Models.QDBConnection;
using Swashbuckle.AspNetCore.Swagger;
using System.Reflection;
using System.Linq;
using System.IO;
using Querier.Api.Models.Interfaces;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Querier.Api.Services
{
    public static class AssemblyLoader
    {
        public static async Task LoadAssemblyFromQDBConnection(
            QDBConnection connection,
            IServiceProvider serviceProvider,
            ApplicationPartManager partManager,
            ILogger logger)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => Path.GetFileName(a.Location))
                .ToList();

            string file = Path.Combine("Assemblies", $"{connection.Name}.DynamicContext.dll");
            if (File.Exists(file))
            {
                var fileName = Path.GetFileName(file);
                if (!loadedAssemblies.Contains(fileName))
                {
                    try
                    {
                        var assembly = Assembly.LoadFrom(file);
                        var dynamicInterfaceType = typeof(IDynamicContextProceduresServicesResolver);
                        
                        if (assembly.GetTypes().Any(t => dynamicInterfaceType.IsAssignableFrom(t)))
                        {
                            var resolverTypes = assembly.GetTypes()
                                .Where(t => dynamicInterfaceType.IsAssignableFrom(t))
                                .ToList();

                            if (resolverTypes.Count != 1)
                            {
                                logger.LogWarning($"Assembly {fileName} contains {resolverTypes.Count} implementations of IDynamicContextProceduresServicesResolver. Skipping.");
                                
                            }

                            var resolverType = resolverTypes.First();
                            var resolver = (IDynamicContextProceduresServicesResolver)Activator.CreateInstance(resolverType);
                            
                            // Ajouter au DynamicContextList
                            var dynamicContextList = DynamicContextList.Instance;
                            
                            resolver.ConfigureServices((IServiceCollection)serviceProvider.GetService(typeof(IServiceCollection)), connection.ConnectionString);
                            var dynamicContextListService = serviceProvider.GetRequiredService<IDynamicContextList>();
                            Console.WriteLine($"Adding DynamicContext {connection.Name}");
                            dynamicContextListService.DynamicContexts.Add(connection.Name, resolver);
                            
                            foreach (KeyValuePair<Type, Type> service in resolver.ProceduresServices)
                            {
                                Console.WriteLine($"Registering service {service.Key}");
                                serviceProvider.GetRequiredService<IServiceCollection>().AddSingleton(service.Key, service.Value);
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
                            
                            logger.LogInformation($"Successfully loaded assembly {fileName} for context {connection.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error loading assembly {fileName}");
                    }
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
    }
} 