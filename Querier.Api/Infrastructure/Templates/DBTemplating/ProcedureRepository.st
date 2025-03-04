using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Infrastructure.Database.Parameters;
using $rootNamespace$.DTOs;
using $contextNamespace$;

namespace $rootNamespace$.Repositories
{
    $procedureList: {procedure|
    #region $procedure.CSName$
    /// <summary>
    /// Repository interface for $procedure.CSName$ stored procedure
    /// </summary>
    public interface I$procedure.CSName$Repository
    {
        /// <summary>
        /// Executes the $procedure.CSName$ stored procedure
        /// </summary>
        $if(procedure.HasParameters)$
        Task<List<$procedure.CSName$Dto>> ExecuteAsync($procedure.CSName$InputDto parameters);
        $else$
        Task<List<$procedure.CSName$Dto>> ExecuteAsync();
        $endif$
    \}

    /// <summary>
    /// Repository implementation for $procedure.CSName$ stored procedure
    /// </summary>
    public class $procedure.CSName$Repository : I$procedure.CSName$Repository
    {
        private readonly IDbContextFactory<$contextName$> _contextFactory;
        private readonly ILogger<$procedure.CSName$Repository> _logger;

        public $procedure.CSName$Repository(
            IDbContextFactory<$contextName$> contextFactory,
            ILogger<$procedure.CSName$Repository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("Initializing $procedure.CSName$Repository");
        \}

        $if(procedure.HasParameters)$
        public async Task<List<$procedure.CSName$Dto>> ExecuteAsync($procedure.CSName$InputDto parameters)
        {
            if (parameters == null)
            {
                _logger.LogWarning("Attempt to execute $procedure.CSName$ with null parameters");
                throw new ArgumentNullException(nameof(parameters));
            \}

            _logger.LogDebug("Executing $procedure.CSName$ stored procedure with parameters: " + parameters.ToString());
        $else$
        public async Task<List<$procedure.CSName$Dto>> ExecuteAsync()
        {
            _logger.LogDebug("Executing $procedure.CSName$ stored procedure");
        $endif$

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                $if(procedure.HasParameters)$
                return await context.Procedures.$procedure.CSName$Async(parameters);
                $else$
                return await context.Procedures.$procedure.CSName$Async();
                $endif$
            \}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing $procedure.CSName$ stored procedure");
                throw;
            \}
        \}
    \}
    #endregion
    }$
\} 