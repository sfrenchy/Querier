using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Linq;

namespace Querier.Api.Infrastructure.Swagger.Filters
{
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                schema.Enum.Clear();
                schema.Type = "string";
                schema.Format = null;
                
                var enumValues = Enum.GetNames(context.Type);
                schema.Enum = enumValues.Select(name => new OpenApiString(name)).ToList<IOpenApiAny>();
                
                // Ajouter une description avec toutes les valeurs possibles
                schema.Description += string.IsNullOrEmpty(schema.Description) 
                    ? $"Valeurs possibles : {string.Join(", ", enumValues)}"
                    : $"\nValeurs possibles : {string.Join(", ", enumValues)}";
            }
        }
    }
} 