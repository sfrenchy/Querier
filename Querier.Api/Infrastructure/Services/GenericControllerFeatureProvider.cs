using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Querier.Api.Infrastructure.Services
{
    public class GenericControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            // Pour chaque assembly part
            foreach (var part in parts)
            {
                if (part is AssemblyPart assemblyPart)
                {
                    var assembly = assemblyPart.Assembly;
                    var types = assembly.GetTypes();

                    // Ajouter tous les types qui sont des contrôleurs
                    foreach (var type in types)
                    {
                        if (IsController(type) && !feature.Controllers.Contains(type.GetTypeInfo()))
                        {
                            feature.Controllers.Add(type.GetTypeInfo());
                        }
                    }
                }
            }
        }

        private bool IsController(Type type)
        {
            // Un type est un contrôleur s'il est une classe publique, non abstraite,
            // et que son nom se termine par "Controller"
            return type.IsPublic 
                && !type.IsAbstract 
                && type.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase);
        }
    }
} 