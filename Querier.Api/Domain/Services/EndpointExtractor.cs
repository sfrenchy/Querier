using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;

namespace Querier.Api.Domain.Services
{
    public class EndpointExtractor
    {
        private readonly JsonSchemaGenerator _schemaGenerator;
        private readonly ILogger<EndpointExtractor> _logger;

        public EndpointExtractor(JsonSchemaGenerator schemaGenerator, ILogger<EndpointExtractor> logger)
        {
            _schemaGenerator = schemaGenerator ?? throw new ArgumentNullException(nameof(schemaGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<EndpointDescription> ExtractFromAssembly(Assembly assembly)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(assembly);
                _logger.LogInformation("Starting endpoint extraction from assembly: {AssemblyName}", assembly.FullName);

                var endpoints = new List<EndpointDescription>();
                var controllers = assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t));
                
                foreach (var controller in controllers)
                {
                    try
                    {
                        _logger.LogDebug("Processing controller: {ControllerName}", controller.Name);
                        var controllerRoute = controller.GetCustomAttributes<RouteAttribute>()
                            .FirstOrDefault()?.Template ?? string.Empty;

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

                                var endpoint = new EndpointDescription
                                {
                                    Action = action.Name,
                                    HttpMethod = string.Join(", ", httpMethods),
                                    Route = CombineRoutes(controllerRoute, actionRoute),
                                    Responses = GetResponses(action).ToList(),
                                    Description = action.GetCustomAttribute<SummaryAttribute>()?.Summary ?? string.Empty
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

        private IEnumerable<EndpointParameter> GetParameters(MethodInfo action)
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
                            JsonSchema = _schemaGenerator.GenerateSchema(param.ParameterType)
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

        private IEnumerable<EndpointResponse> GetResponses(MethodInfo action)
        {
            try
            {
                _logger.LogTrace("Getting responses for action: {ActionName}", action.Name);
                var responses = new List<EndpointResponse>();

                // Add success response
                var produces = action.GetCustomAttribute<ProducesResponseTypeAttribute>();
                if (produces != null)
                {
                    responses.Add(new EndpointResponse
                    {
                        StatusCode = produces.StatusCode,
                        Description = "Successful response",
                        JsonSchema = _schemaGenerator.GenerateSchema(produces.Type)
                    });
                    _logger.LogTrace("Added success response with status code {StatusCode} for action {ActionName}", 
                        produces.StatusCode, action.Name);
                }

                // Add error response
                responses.Add(new EndpointResponse
                {
                    StatusCode = 400,
                    Description = "Bad Request",
                    JsonSchema = GenerateErrorSchema()
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