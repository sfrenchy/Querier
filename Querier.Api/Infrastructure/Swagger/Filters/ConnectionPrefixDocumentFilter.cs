using System.Linq;
using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Querier.Api.Infrastructure.Swagger.Filters
{
    public class ConnectionPrefixDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var paths = swaggerDoc.Paths.ToList();
            swaggerDoc.Paths.Clear();

            var tagsByPrefix = new Dictionary<string, HashSet<string>>();
            var defaultTags = new HashSet<string>();

            // Première passe : collecter tous les tags
            foreach (var path in paths)
            {
                var segments = path.Key.Split('/', System.StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 3 && segments[0] == "api" && segments[1] == "v1")
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
                if (segments.Length >= 3 && segments[0] == "api" && segments[1] == "v1")
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
                orderedTags.Add(new OpenApiTag 
                { 
                    Name = tag,
                    Description = $"Endpoints for {tag} database connection"
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