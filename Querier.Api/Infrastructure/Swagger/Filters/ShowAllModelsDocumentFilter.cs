using System;
using System.Linq;
using Microsoft.OpenApi.Models;
using Querier.Api.Infrastructure.Swagger.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Querier.Api.Infrastructure.Swagger.Filters
{
    /// <summary>
    /// Filtre Swagger pour afficher tous les modèles dans la documentation,
    /// même s'ils ne sont pas directement référencés par les endpoints
    /// </summary>
    public class ShowAllModelsDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var type in context.SchemaGenerator.GetTypesInAssembly(typeof(ShowAllModelsDocumentFilter).Assembly))
            {
                if (type.Namespace?.StartsWith("Querier.Api.Application.DTOs") == true)
                {
                    var schema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);
                }
            }
        }
    }
} 