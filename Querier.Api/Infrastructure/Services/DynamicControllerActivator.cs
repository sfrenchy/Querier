using System;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<Type, bool> _dynamicControllerCache;
        private readonly ConcurrentDictionary<Type, ConstructorInfo> _constructorCache;
        private readonly ConcurrentDictionary<ConstructorInfo, ParameterInfo[]> _parameterCache;

        public DynamicControllerActivator(
            IServiceProvider serviceProvider,
            IAssemblyManagerService assemblyManager,
            ILogger<DynamicControllerActivator> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _assemblyManager = assemblyManager ?? throw new ArgumentNullException(nameof(assemblyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dynamicControllerCache = new ConcurrentDictionary<Type, bool>();
            _constructorCache = new ConcurrentDictionary<Type, ConstructorInfo>();
            _parameterCache = new ConcurrentDictionary<ConstructorInfo, ParameterInfo[]>();
        }

        private (ConstructorInfo Constructor, ParameterInfo[] Parameters) GetConstructorInfo(Type controllerType)
        {
            var constructor = _constructorCache.GetOrAdd(controllerType, type => 
                type.GetConstructors().First());
            
            var parameters = _parameterCache.GetOrAdd(constructor, ctor => 
                ctor.GetParameters());

            return (constructor, parameters);
        }

        private object[] ResolveConstructorParameters(ParameterInfo[] parameters, IServiceProvider scopedProvider)
        {
            var resolvedParameters = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                try
                {
                    var service = scopedProvider.GetRequiredService(parameter.ParameterType);
                    if (service == null)
                    {
                        throw new InvalidOperationException($"Service {parameter.ParameterType.FullName} was resolved but is null");
                    }
                    resolvedParameters[i] = service;

                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(
                            "Service resolution for {ParameterType}: Available and non-null",
                            parameter.ParameterType.FullName
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error resolving service {ParameterType}",
                        parameter.ParameterType.FullName
                    );
                    throw;
                }
            }

            return resolvedParameters;
        }

        public object Create(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var controllerType = context.ActionDescriptor.ControllerTypeInfo;

            // Fast path - Vérifie si nous avons déjà déterminé si c'est un contrôleur dynamique
            if (!_dynamicControllerCache.GetOrAdd(controllerType, type =>
            {
                var assemblyName = type.Assembly.GetName().Name;
                return _assemblyManager.IsAssemblyLoaded(_assemblyManager.GetContextNormalizedAssemblyName(assemblyName));
            }))
            {
                // Fast path pour les contrôleurs natifs
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Using fast path for native controller: {ControllerType}", controllerType.FullName);
                }
                return ActivatorUtilities.CreateInstance(context.HttpContext.RequestServices, controllerType);
            }

            // Le reste du code pour les contrôleurs dynamiques
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Creating dynamic controller of type: {ControllerType}", controllerType.FullName);
            }

            var assemblyName = controllerType.Assembly.GetName().Name;
            var normalizedName = _assemblyManager.GetContextNormalizedAssemblyName(assemblyName);
            var container = _assemblyManager.GetServiceContainer(normalizedName);

            if (container == null)
            {
                _logger.LogWarning("Service container is null for {AssemblyName}", normalizedName);
                return ActivatorUtilities.CreateInstance(_serviceProvider, controllerType);
            }

            using var scope = container.ServiceProvider.CreateScope();
            var scopedProvider = scope.ServiceProvider;

            var (constructor, parameters) = GetConstructorInfo(controllerType);
            var resolvedParameters = ResolveConstructorParameters(parameters, scopedProvider);

            try
            {
                if (resolvedParameters.Any(p => p == null))
                {
                    throw new InvalidOperationException("One or more constructor parameters are null");
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Creating controller {ControllerType} with {ParameterCount} parameters",
                        controllerType.FullName,
                        resolvedParameters.Length
                    );
                }

                var controller = constructor.Invoke(resolvedParameters);
                
                if (controller == null)
                {
                    throw new InvalidOperationException($"Failed to create controller instance of type {controllerType.FullName}");
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug(
                        "Successfully created controller instance of type {ControllerType}",
                        controllerType.FullName
                    );
                }

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

        public void Release(ControllerContext context, object controller)
        {
            if (controller is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
} 