using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Querier.Api.Infrastructure.Swagger.Extensions
{
    public static class SwaggerGenOptionsExtensions
    {
        public static IEnumerable<Type> GetTypesInAssembly(this ISchemaGenerator schemaGenerator, Assembly assembly)
        {
            return assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract && !type.IsGenericType)
                .OrderBy(type => type.Name);
        }
    }
} 