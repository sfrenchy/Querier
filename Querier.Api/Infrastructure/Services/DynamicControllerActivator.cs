using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Infrastructure;

namespace Querier.Api.Infrastructure.Services
{
    public class DynamicControllerActivator : IControllerActivator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAssemblyManagerService _assemblyManager;
        private readonly ILogger<DynamicControllerActivator> _logger;

        public DynamicControllerActivator(
            IServiceProvider serviceProvider,
            IAssemblyManagerService assemblyManager,
            ILogger<DynamicControllerActivator> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _assemblyManager = assemblyManager ?? throw new ArgumentNullException(nameof(assemblyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public object Create(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var controllerType = context.ActionDescriptor.ControllerTypeInfo;
            _logger.LogDebug("Creating controller of type: {ControllerType}", controllerType.FullName);

            var assemblyName = controllerType.Assembly.GetName().Name;
            _logger.LogDebug("Controller assembly name: {AssemblyName}", assemblyName);

            var normalizedName = NormalizeAssemblyName(assemblyName);
            _logger.LogDebug("Normalized assembly name: {NormalizedName}", normalizedName);

            if (_assemblyManager.IsAssemblyLoaded(normalizedName))
            {
                _logger.LogDebug("Assembly {AssemblyName} is dynamically loaded, using its service provider", normalizedName);
                var container = _assemblyManager.GetServiceContainer(normalizedName);
                if (container != null)
                {
                    _logger.LogDebug("Service container found for {AssemblyName}", normalizedName);
                    var serviceProvider = container.ServiceProvider;

                    // Créer un scope pour la résolution des services
                    using var scope = serviceProvider.CreateScope();
                    var scopedProvider = scope.ServiceProvider;

                    // D'abord, vérifions les services enregistrés
                    if (serviceProvider is ServiceProvider sp)
                    {
                        var services = sp.GetRequiredService<IServiceCollection>();
                        _logger.LogDebug("Total services registered: {Count}", services.Count);
                        foreach (var service in services)
                        {
                            _logger.LogDebug(
                                "Registered service: {ServiceType} => {ImplementationType} ({Lifetime})",
                                service.ServiceType.FullName,
                                service.ImplementationType?.FullName ?? "Unknown",
                                service.Lifetime
                            );
                        }
                    }

                    // Ensuite, essayons de résoudre les services requis par le constructeur
                    var constructor = controllerType.GetConstructors()[0];
                    var parameters = new object[constructor.GetParameters().Length];
                    var parameterInfo = constructor.GetParameters();

                    for (var i = 0; i < parameterInfo.Length; i++)
                    {
                        var parameter = parameterInfo[i];
                        try
                        {
                            // Utiliser le service provider scopé pour résoudre les services
                            var service = scopedProvider.GetRequiredService(parameter.ParameterType);
                            if (service == null)
                            {
                                throw new InvalidOperationException($"Service {parameter.ParameterType.FullName} was resolved but is null");
                            }
                            parameters[i] = service;
                            _logger.LogDebug(
                                "Service resolution for {ParameterType}: Available and non-null",
                                parameter.ParameterType.FullName
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Error resolving service {ParameterType}",
                                parameter.ParameterType.FullName
                            );
                            throw; // Propager l'erreur pour éviter une instanciation partielle
                        }
                    }

                    try
                    {
                        // Vérifier que tous les paramètres sont non-null avant de créer le contrôleur
                        if (parameters.Any(p => p == null))
                        {
                            throw new InvalidOperationException("One or more constructor parameters are null");
                        }

                        _logger.LogDebug(
                            "Creating controller {ControllerType} with {ParameterCount} parameters",
                            controllerType.FullName,
                            parameters.Length
                        );

                        // Créer l'instance du contrôleur avec les paramètres résolus
                        var controller = constructor.Invoke(parameters);
                        
                        if (controller == null)
                        {
                            throw new InvalidOperationException($"Failed to create controller instance of type {controllerType.FullName}");
                        }

                        _logger.LogDebug(
                            "Successfully created controller instance of type {ControllerType}",
                            controllerType.FullName
                        );

                        return controller;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Error creating controller instance of type {ControllerType}",
                            controllerType.FullName
                        );
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning("Service container is null for {AssemblyName}", normalizedName);
                }
            }

            _logger.LogDebug("Using default service provider for controller {ControllerType}", controllerType.FullName);
            return ActivatorUtilities.CreateInstance(_serviceProvider, controllerType);
        }

        public void Release(ControllerContext context, object controller)
        {
            if (controller is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        private string NormalizeAssemblyName(string assemblyName)
        {
            // Enlever l'extension .dll si présente
            if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = assemblyName.Substring(0, assemblyName.Length - 4);
            }

            // Si le nom contient _DataContext, le retirer
            if (assemblyName.EndsWith("_DataContext", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = assemblyName.Substring(0, assemblyName.Length - 12);
            }

            return assemblyName;
        }
    }
} 