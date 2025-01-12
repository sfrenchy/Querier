using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Xml.Linq;
using System.Reflection;
using Querier.Api.Application.Interfaces.Services;

namespace Querier.Api.Infrastructure.Swagger.Filters
{
    public class ConnectionPrefixDocumentFilter : IDocumentFilter
    {
        private readonly string _xmlPath;
        private readonly IDbConnectionService _dbConnectionService;

        public ConnectionPrefixDocumentFilter(IDbConnectionService dbConnectionService)
        {
            _dbConnectionService = dbConnectionService;
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            _xmlPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
        }

        private async Task<IEnumerable<string>> GetDatabaseConnections()
        {
            var connections = await _dbConnectionService.GetAllAsync();
            return connections.Select(c => c.ApiRoute).ToList();
        }

        private string GetControllerDescription(TypeInfo controllerType)
        {
            if (!File.Exists(_xmlPath)) return null;

            var doc = XDocument.Load(_xmlPath);
            var memberName = $"T:{controllerType.FullName}";
            var summaryNode = doc.Descendants("member")
                .FirstOrDefault(m => m.Attribute("name")?.Value == memberName)?
                .Element("summary");

            return summaryNode?.Value.Trim();
        }

        public async void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var databaseConnections = await GetDatabaseConnections();
            var paths = swaggerDoc.Paths.ToList();
            swaggerDoc.Paths.Clear();

            var tagsByPrefix = new Dictionary<string, HashSet<string>>();
            var defaultTags = new HashSet<string>();

            // Première passe : collecter tous les tags
            foreach (var path in paths)
            {
                var segments = path.Key.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 3 && segments[0] == "api" && segments[1] == "v1" && 
                    databaseConnections.Contains(segments[2]))
                {
                    var connectionPrefix = segments[2];
                    foreach (var operation in path.Value.Operations)
                    {
                        var controllerName = operation.Value.Tags.FirstOrDefault()?.Name;
                        if (controllerName != null)
                        {
                            if (!tagsByPrefix.ContainsKey(connectionPrefix))
                            {
                                tagsByPrefix[connectionPrefix] = new HashSet<string>();
                            }
                            tagsByPrefix[connectionPrefix].Add(controllerName);
                        }
                    }
                }
                else
                {
                    foreach (var operation in path.Value.Operations)
                    {
                        var controllerName = operation.Value.Tags.FirstOrDefault()?.Name;
                        if (controllerName != null)
                        {
                            defaultTags.Add(controllerName);
                        }
                    }
                }
            }

            // Deuxième passe : mettre à jour les tags des opérations
            foreach (var path in paths)
            {
                var segments = path.Key.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 3 && segments[0] == "api" && segments[1] == "v1" && 
                    databaseConnections.Contains(segments[2]))
                {
                    var connectionPrefix = segments[2];
                    foreach (var operation in path.Value.Operations)
                    {
                        var controllerName = operation.Value.Tags.FirstOrDefault()?.Name;
                        if (controllerName != null)
                        {
                            operation.Value.Tags.Clear();
                            operation.Value.Tags.Add(new OpenApiTag { Name = connectionPrefix });
                        }
                    }
                }
                swaggerDoc.Paths.Add(path.Key, path.Value);
            }

            // Créer les tags dans l'ordre souhaité
            var orderedTags = new List<OpenApiTag>();

            // Ajouter d'abord les tags par défaut
            foreach (var tag in defaultTags.OrderBy(t => t))
            {
                var controllerType = context.ApiDescriptions
                    .Select(d => (d.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo)
                    .FirstOrDefault(c => c != null && c.Name.Replace("Controller", "") == tag);

                var description = controllerType != null ? GetControllerDescription(controllerType) : null;

                orderedTags.Add(new OpenApiTag 
                { 
                    Name = tag,
                    Description = description ?? $"No description available for {tag}"
                });
            }

            // Ajouter les tags de connexion
            foreach (var prefix in tagsByPrefix.Keys.OrderBy(k => k))
            {
                var controllers = tagsByPrefix[prefix].OrderBy(c => c);
                var description = $"Endpoints for {prefix} database connection";

                orderedTags.Add(new OpenApiTag 
                { 
                    Name = prefix,
                    Description = description,
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        ["x-controllers"] = new OpenApiString(string.Join(",", controllers))
                    }
                });
            }

            swaggerDoc.Tags = orderedTags;
        }
    }
} 