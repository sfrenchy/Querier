using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    /// <summary>
    /// Interface principale pour la gestion des services dynamiques
    /// </summary>
    public interface IDynamicContextServiceContainer : 
        IDynamicContextEntityServicesResolver,
        IDynamicContextProceduresServicesResolver
    {
        /// <summary>
        /// Obtient un service du type spécifié
        /// </summary>
        T GetService<T>() where T : class;

        /// <summary>
        /// Obtient tous les services du type spécifié
        /// </summary>
        IEnumerable<T> GetServices<T>() where T : class;

        /// <summary>
        /// Crée un nouveau scope de services
        /// </summary>
        IServiceScope CreateScope();

        /// <summary>
        /// Configure tous les services pour le contexte dynamique
        /// </summary>
        void ConfigureServices(IServiceCollection services, DbConnectionType connectionType, string connectionString, ILogger logger);

        /// <summary>
        /// Valide que tous les services nécessaires sont correctement configurés
        /// </summary>
        bool ValidateConfiguration();
    }
}