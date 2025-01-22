using System;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    /// <summary>
    /// Interface pour la résolution des services de procédures stockées
    /// </summary>
    public interface IDynamicContextProceduresServicesResolver
    {
        /// <summary>
        /// Obtient le type de service de procédure pour le nom spécifié
        /// </summary>
        Type GetProcedureServiceType(string procedureName);

        /// <summary>
        /// Enregistre un mapping de type de service de procédure
        /// </summary>
        void RegisterProcedureServiceType(string procedureName, Type serviceType);

        /// <summary>
        /// Vérifie si un service de procédure existe pour le nom spécifié
        /// </summary>
        bool HasProcedureService(string procedureName);
    }
}