using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;

namespace Querier.Api.Domain.Services
{
    public class EndpointExtractor
    {
        private readonly JsonSchemaGenerator _schemaGenerator;

        public EndpointExtractor(JsonSchemaGenerator schemaGenerator)
        {
            _schemaGenerator = schemaGenerator;
        }

        public List<EndpointDescription> ExtractFromAssembly(Assembly assembly)
        {
            var endpoints = new List<EndpointDescription>();
            
            foreach (var controller in assembly.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t)))
            {
                var controllerRoute = controller.GetCustomAttributes<RouteAttribute>()
                    .FirstOrDefault()?.Template ?? string.Empty;

                foreach (var action in controller.GetMethods())
                {
                    var httpMethods = GetHttpMethods(action);
                    if (!httpMethods.Any()) continue;

                    var actionRoute = action.GetCustomAttributes<RouteAttribute>()
                        .FirstOrDefault()?.Template ?? string.Empty;

                    endpoints.Add(new EndpointDescription
                    {
                        Controller = controller.Name,
                        Action = action.Name,
                        HttpMethod = string.Join(", ", httpMethods),
                        Route = CombineRoutes(controllerRoute, actionRoute),
                        Description = action.GetCustomAttribute<SummaryAttribute>()?.Summary ?? string.Empty,
                        Parameters = ExtractParameters(action).ToList(),
                        Responses = ExtractResponses(action).ToList()
                    });
                }
            }

            return endpoints;
        }

        private IEnumerable<string> GetHttpMethods(MethodInfo action)
        {
            return action.GetCustomAttributes()
                .Where(a => a is HttpGetAttribute || 
                           a is HttpPostAttribute || 
                           a is HttpPutAttribute || 
                           a is HttpDeleteAttribute)
                .Select(a => a switch
                {
                    HttpGetAttribute => "GET",
                    HttpPostAttribute => "POST",
                    HttpPutAttribute => "PUT",
                    HttpDeleteAttribute => "DELETE",
                    _ => "GET"
                });
        }

        private IEnumerable<EndpointParameter> ExtractParameters(MethodInfo method)
        {
            foreach (var param in method.GetParameters())
            {
                var fromBody = param.GetCustomAttribute<FromBodyAttribute>();
                var fromQuery = param.GetCustomAttribute<FromQueryAttribute>();
                var fromRoute = param.GetCustomAttribute<FromRouteAttribute>();
                var required = param.GetCustomAttribute<RequiredAttribute>();

                yield return new EndpointParameter
                {
                    Name = param.Name,
                    Type = param.ParameterType.Name,
                    Description = param.GetCustomAttribute<SummaryAttribute>()?.Summary ?? string.Empty,
                    IsRequired = required != null || !param.IsOptional,
                    Source = GetParameterSource(fromBody, fromQuery, fromRoute),
                    JsonSchema = _schemaGenerator.GenerateSchema(param.ParameterType)
                };
            }
        }

        private string GetParameterSource(FromBodyAttribute fromBody, FromQueryAttribute fromQuery, FromRouteAttribute fromRoute)
        {
            if (fromBody != null) return "FromBody";
            if (fromQuery != null) return "FromQuery";
            if (fromRoute != null) return "FromRoute";
            return "FromQuery";
        }

        private IEnumerable<EndpointResponse> ExtractResponses(MethodInfo method)
        {
            var produces = method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                .Where(attr => attr.StatusCode == 200)
                .ToList();
            
            foreach (var response in produces)
            {
                yield return new EndpointResponse
                {
                    StatusCode = response.StatusCode,
                    Type = response.Type?.Name ?? "void",
                    Description = "Success",
                    JsonSchema = response.Type != null 
                        ? _schemaGenerator.GenerateSchema(response.Type) 
                        : GenerateErrorSchema()
                };
            }
        }

        private string GenerateErrorSchema()
        {
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

            return System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        }

        private string CombineRoutes(params string[] routes)
        {
            return string.Join("/", routes
                .Where(r => !string.IsNullOrEmpty(r))
                .Select(r => r.Trim('/'))
            );
        }
    }
} 