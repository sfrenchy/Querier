using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Enums;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    /// <summary>
    /// Interface pour le résolveur de services d'entités générés dynamiquement
    /// </summary>
    public interface IDynamicContextEntityServicesResolver
    {
        /// <summary>
        /// Mapping entre les interfaces de service et leurs implémentations
        /// </summary>
        Dictionary<Type, Type> EntityServices { get; }

        /// <summary>
        /// Mapping entre les noms d'entités et leurs interfaces de service
        /// </summary>
        Dictionary<string, Type> EntityNameService { get; }

        /// <summary>
        /// Nom du contexte dynamique
        /// </summary>
        string DynamicContextName { get; }

        /// <summary>
        /// Configure les services pour le contexte dynamique
        /// </summary>
        void ConfigureServices(IServiceCollection services, DbConnectionType ConnectionType, string connectionString, ILogger logger);
    }
} 