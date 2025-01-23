using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.DBConnection;

namespace Querier.Api.Application.Interfaces.Infrastructure
{
    /// <summary>
    /// Gère le cycle de vie des assemblies dynamiques et leurs conteneurs de services
    /// </summary>
    public interface IAssemblyManagerService
    {
        /// <summary>
        /// Normalise le nom d'une assembly en retirant les suffixes et extensions non nécessaires
        /// </summary>
        /// <param name="assemblyName">Nom de l'assembly à normaliser</param>
        /// <returns>Le nom normalisé de l'assembly</returns>
        string GetNormalizedAssemblyName(string assemblyName);

        /// <summary>
        /// Charge une assembly à partir d'une connexion de base de données
        /// </summary>
        /// <param name="connection">La connexion de base de données</param>
        /// <returns>Le conteneur de services pour l'assembly chargée</returns>
        Task<IDynamicContextServiceContainer> LoadAssemblyAsync(DBConnection connection);

        /// <summary>
        /// Charge une assembly à partir de bytes
        /// </summary>
        /// <param name="name">Nom de l'assembly</param>
        /// <param name="connectionType">Type de connexion</param>
        /// <param name="connectionString">Chaîne de connexion</param>
        /// <param name="assemblyBytes">Contenu de l'assembly</param>
        /// <returns>Le conteneur de services pour l'assembly chargée</returns>
        Task<IDynamicContextServiceContainer> LoadAssemblyAsync(string name, DbConnectionType connectionType, string connectionString, byte[] assemblyBytes);

        /// <summary>
        /// Charge une assembly à partir d'un fichier
        /// </summary>
        /// <param name="name">Nom de l'assembly</param>
        /// <param name="connectionType">Type de connexion</param>
        /// <param name="connectionString">Chaîne de connexion</param>
        /// <param name="assemblyPath">Chemin du fichier de l'assembly</param>
        /// <returns>Le conteneur de services pour l'assembly chargée</returns>
        Task<IDynamicContextServiceContainer> LoadAssemblyFromFileAsync(string name, DbConnectionType connectionType, string connectionString, string assemblyPath);

        /// <summary>
        /// Décharge une assembly
        /// </summary>
        /// <param name="name">Nom de l'assembly à décharger</param>
        Task UnloadAssemblyAsync(string name);

        /// <summary>
        /// Obtient le conteneur de services pour une assembly
        /// </summary>
        /// <param name="name">Nom de l'assembly</param>
        /// <returns>Le conteneur de services ou null si non trouvé</returns>
        IDynamicContextServiceContainer GetServiceContainer(string name);

        /// <summary>
        /// Vérifie si une assembly est chargée
        /// </summary>
        /// <param name="name">Nom de l'assembly</param>
        /// <returns>True si l'assembly est chargée, false sinon</returns>
        bool IsAssemblyLoaded(string name);
    }
} 