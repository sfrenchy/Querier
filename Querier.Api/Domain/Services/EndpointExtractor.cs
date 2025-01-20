using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;
using Querier.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Common.Attributes;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Domain.Services
{
    public class EndpointExtractor
    {
        private readonly JsonSchemaGeneratorService _schemaGenerator;
        private readonly ILogger<EndpointExtractor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceCollection _serviceCollection;

        public EndpointExtractor(JsonSchemaGeneratorService schemaGenerator, ILogger<EndpointExtractor> logger, IServiceProvider serviceProvider, IServiceCollection serviceCollection)
        {
            _schemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _serviceCollection = serviceCollection;
        }

        private string GetDbContextTypeName(Assembly assembly)
        {
            try
            {
                var dbContextType = assembly.GetTypes()
                    .FirstOrDefault(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t));

                if (dbContextType == null)
                {
                    _logger.LogWarning("No DbContext found in assembly {AssemblyName}", assembly.FullName);
                    return null;
                }

                _logger.LogDebug("Found DbContext type: {DbContextType}", dbContextType.FullName);
                return dbContextType.FullName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DbContext type name from assembly {AssemblyName}", assembly?.FullName);
                return null;
            }
        }

        private DbContext GetDbContextForType(DbContext context, Type type)
        {
            try
            {
                // Trouver le DbContext dans le même assembly que le type
                var assembly = type.Assembly;
                var dbContextType = assembly.GetTypes()
                    .FirstOrDefault(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t));

                if (dbContextType == null)
                {
                    _logger.LogWarning("No DbContext found in assembly {AssemblyName} for type {TypeName}", 
                        assembly.FullName, type.FullName);
                    return null;
                }

                // Créer une instance du DbContext trouvé
                
                var dbContext = ActivatorUtilities.CreateInstance(_serviceProvider, dbContextType) as DbContext;
                _logger.LogDebug("Created DbContext of type {DbContextType} for type {TypeName}", 
                    dbContextType.Name, type.FullName);
                return dbContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DbContext for type {TypeName}", type?.FullName);
                return null;
            }
        }

        private Type GetIntrinsicType(Type type)
        {
            if (type == null) return null;

            // Si c'est une liste ou une collection, on récupère le type générique
            if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || 
                type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                type.GetGenericTypeDefinition() == typeof(ICollection<>)))
            {
                return type.GetGenericArguments()[0];
            }

            // Si c'est un type générique (comme PagedResult<T>), on récupère le type générique
            if (type.IsGenericType && type.GetGenericArguments().Length == 1)
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }

        public List<EndpointDescription> ExtractFromAssembly(Assembly assembly, string connectionString, DbConnectionType connectionType)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(assembly);
                _logger.LogInformation("Starting endpoint extraction from assembly: {AssemblyName}", assembly.FullName);

                var endpoints = new List<EndpointDescription>();
                var controllers = assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t));
                var contextTypeName = GetDbContextTypeName(assembly);
                var dbContextType = assembly.GetTypes()
                    .FirstOrDefault(t => !t.IsAbstract && typeof(DbContext).IsAssignableFrom(t));

                DbContext dbContext = Utils.GetDbContextFromTypeName(dbContextType.FullName, connectionString, connectionType);
                
                foreach (var controller in controllers)
                {
                    try
                    {
                        
                        _logger.LogDebug("Processing controller: {ControllerName}", controller.Name);
                        var controllerRoute = controller.GetCustomAttributes<RouteAttribute>()
                            .FirstOrDefault()?.Template ?? string.Empty;
                        var controllerTargetTable = controller.GetCustomAttributes<ControllerFor>()
                            .FirstOrDefault()?.Table ?? string.Empty;
                        foreach (var action in controller.GetMethods())
                        {
                            try
                            {
                                var httpMethods = GetHttpMethods(action);
                                if (!httpMethods.Any())
                                {
                                    _logger.LogTrace("Skipping action {ActionName} in {ControllerName} - no HTTP methods found", 
                                        action.Name, controller.Name);
                                    continue;
                                }

                                _logger.LogTrace("Processing action {ActionName} in {ControllerName}", 
                                    action.Name, controller.Name);

                                var actionRoute = action.GetCustomAttributes<RouteAttribute>()
                                    .FirstOrDefault()?.Template ?? string.Empty;

                                // Récupérer le type de retour depuis l'attribut ProducesResponseType avec un code 200
                                var producesAttribute = action.GetCustomAttributes<ProducesResponseTypeAttribute>()
                                    .FirstOrDefault(a => a.StatusCode == 200 || a.StatusCode == 201);
                                
                                Type returnType = null;
                                if (producesAttribute != null)
                                {
                                    returnType = GetIntrinsicType(producesAttribute.Type);
                                }

                                var endpoint = new EndpointDescription
                                {
                                    Action = action.Name,
                                    Controller = controller.Name,
                                    HttpMethod = string.Join(", ", httpMethods),
                                    TargetTable = controllerTargetTable,
                                    Route = CombineRoutes(controllerRoute.Replace("api/v1/", ""), actionRoute),
                                    Parameters = GetParameters(dbContext, action).ToList(),
                                    Responses = GetResponses(action, contextTypeName, connectionString, connectionType).ToList(),
                                    Description = action.GetCustomAttribute<SummaryAttribute>()?.Summary ?? string.Empty,
                                    EntitySubjectJsonSchema = returnType != null ? _schemaGenerator.GenerateFromType(returnType, Utils.GetDbContextFromTypeName(contextTypeName, connectionString, connectionType)) : "{}"
                                };

                                _logger.LogDebug("Extracted endpoint {EndpointName} with {ParameterCount} parameters and {ResponseCount} responses", 
                                    endpoint.Route, endpoint.Parameters.Count, endpoint.Responses.Count);
                                endpoints.Add(endpoint);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing action {ActionName} in {ControllerName}", 
                                    action.Name, controller.Name);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing controller: {ControllerName}", controller.Name);
                    }
                }

                _logger.LogInformation("Completed endpoint extraction. Found {Count} endpoints", endpoints.Count);
                return endpoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting endpoints from assembly: {AssemblyName}", assembly?.FullName);
                throw;
            }
        }

        private IEnumerable<string> GetHttpMethods(MethodInfo action)
        {
            try
            {
                _logger.LogTrace("Getting HTTP methods for action: {ActionName}", action.Name);
                var methods = new List<string>();

                // Check for specific HTTP method attributes
                if (action.GetCustomAttribute<HttpGetAttribute>() != null) methods.Add("GET");
                if (action.GetCustomAttribute<HttpPostAttribute>() != null) methods.Add("POST");
                if (action.GetCustomAttribute<HttpPutAttribute>() != null) methods.Add("PUT");
                if (action.GetCustomAttribute<HttpDeleteAttribute>() != null) methods.Add("DELETE");
                if (action.GetCustomAttribute<HttpPatchAttribute>() != null) methods.Add("PATCH");

                _logger.LogTrace("Found {Count} HTTP methods for action {ActionName}: {Methods}", 
                    methods.Count, action.Name, string.Join(", ", methods));
                return methods;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting HTTP methods for action: {ActionName}", action.Name);
                throw;
            }
        }

        private string CombineRoutes(string controllerRoute, string actionRoute)
        {
            try
            {
                var combined = $"{controllerRoute.TrimEnd('/')}/{actionRoute.TrimStart('/')}".TrimEnd('/');
                _logger.LogTrace("Combined routes: {ControllerRoute} + {ActionRoute} = {CombinedRoute}", 
                    controllerRoute, actionRoute, combined);
                return combined;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error combining routes: {ControllerRoute} and {ActionRoute}", 
                    controllerRoute, actionRoute);
                throw;
            }
        }

        private IEnumerable<EndpointParameter> GetParameters(DbContext dbContext, MethodInfo action)
        {
            try
            {
                _logger.LogTrace("Getting parameters for action: {ActionName}", action.Name);
                var parameters = new List<EndpointParameter>();
                
                foreach (var param in action.GetParameters())
                {
                    try
                    {
                        var fromBody = param.GetCustomAttribute<FromBodyAttribute>();
                        var fromQuery = param.GetCustomAttribute<FromQueryAttribute>();
                        var fromRoute = param.GetCustomAttribute<FromRouteAttribute>();
                        var required = param.GetCustomAttribute<RequiredAttribute>();

                        var parameter = new EndpointParameter
                        {
                            Name = param.Name,
                            Type = param.ParameterType.Name,
                            Description = param.GetCustomAttribute<SummaryAttribute>()?.Summary ?? string.Empty,
                            IsRequired = required != null || !param.IsOptional,
                            Source = GetParameterSource(fromBody, fromQuery, fromRoute),
                            JsonSchema = _schemaGenerator.GenerateFromType(param.ParameterType, dbContext)
                        };

                        _logger.LogTrace("Added parameter {ParameterName} of type {ParameterType} for action {ActionName}", 
                            parameter.Name, parameter.Type, action.Name);
                        parameters.Add(parameter);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing parameter {ParameterName} for action {ActionName}", 
                            param.Name, action.Name);
                    }
                }

                return parameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parameters for action: {ActionName}", action.Name);
                throw;
            }
        }

        private string GetParameterSource(FromBodyAttribute fromBody, FromQueryAttribute fromQuery, FromRouteAttribute fromRoute)
        {
            try
            {
                string source;
                if (fromBody != null) source = "FromBody";
                else if (fromQuery != null) source = "FromQuery";
                else if (fromRoute != null) source = "FromRoute";
                else source = "FromQuery";

                _logger.LogTrace("Determined parameter source: {Source}", source);
                return source;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining parameter source");
                throw;
            }
        }

        private IEnumerable<EndpointResponse> GetResponses(MethodInfo action, string contextTypeName, string connectionString, DbConnectionType connectionType)
        {
            try
            {
                _logger.LogTrace("Getting responses for action: {ActionName}", action.Name);
                var responses = new List<EndpointResponse>();

                // Add success responses
                var produces = action.GetCustomAttributes<ProducesResponseTypeAttribute>(true);
                foreach (var response in produces)
                {
                    Type targetType = GetIntrinsicType(response.Type);
                    responses.Add(new EndpointResponse
                    {
                        StatusCode = response.StatusCode,
                        Description = GetResponseDescription(response.StatusCode),
                        JsonSchema = _schemaGenerator.GenerateFromType(targetType, Utils.GetDbContextFromTypeName(contextTypeName, connectionString, connectionType)),
                        Type = response.Type?.Name ?? "void"
                    });
                    _logger.LogTrace("Added response with status code {StatusCode} for action {ActionName}", 
                        response.StatusCode, action.Name);
                }

                // If no responses defined, add a default 200 response
                if (!responses.Any())
                {
                    responses.Add(new EndpointResponse
                    {
                        StatusCode = 200,
                        Description = "Successful response",
                        JsonSchema = "{}",
                        Type = "void"
                    });
                }

                // Add error response
                responses.Add(new EndpointResponse
                {
                    StatusCode = 400,
                    Description = "Bad Request",
                    JsonSchema = GenerateErrorSchema(),
                    Type = "ErrorResponse"
                });
                _logger.LogTrace("Added error response for action {ActionName}", action.Name);

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting responses for action: {ActionName}", action.Name);
                throw;
            }
        }

        private string GetResponseDescription(int statusCode)
        {
            return statusCode switch
            {
                200 => "Successful response",
                201 => "Resource created successfully",
                204 => "No content",
                400 => "Bad request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Resource not found",
                409 => "Conflict",
                500 => "Internal server error",
                _ => "Response"
            };
        }

        private string GenerateErrorSchema()
        {
            try
            {
                _logger.LogTrace("Generating error schema");
                var schema = new
                {
                    type = "object",
                    properties = new
                    {
                        code = new { type = "string", description = "Code d'erreur" },
                        message = new { type = "string", description = "Message d'erreur" },
                        details = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                properties = new
                                {
                                    field = new { type = "string", description = "Champ concerné par l'erreur" },
                                    message = new { type = "string", description = "Description de l'erreur" }
                                }
                            }
                        }
                    }
                };

                return System.Text.Json.JsonSerializer.Serialize(schema, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating error schema");
                throw;
            }
        }
    }
} 