using System;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    /// <summary>
    /// Interface pour la résolution des services d'entités
    /// </summary>
    public interface IDynamicContextEntityServicesResolver
    {
        /// <summary>
        /// Obtient le type de service d'entité pour le nom spécifié
        /// </summary>
        Type GetEntityServiceType(string entityName);

        /// <summary>
        /// Enregistre un mapping de type de service d'entité
        /// </summary>
        void RegisterEntityServiceType(string entityName, Type serviceType);

        /// <summary>
        /// Vérifie si un service d'entité existe pour le nom spécifié
        /// </summary>
        bool HasEntityService(string entityName);
    }
} 