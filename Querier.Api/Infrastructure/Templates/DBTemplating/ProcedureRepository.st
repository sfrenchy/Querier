using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Infrastructure.Database.Parameters;

namespace $nameSpace$.Repositories
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
        Task<List<$procedure.CSName$Dto>> ExecuteAsync($procedure.CSName$InputDto parameters);
    \}

    /// <summary>
    /// Repository implementation for $procedure.CSName$ stored procedure
    /// </summary>
    public class $procedure.CSName$Repository : I$procedure.CSName$Repository
    {
        private readonly IDbContextFactory<$contextNameSpace$> _contextFactory;
        private readonly ILogger<$procedure.CSName$Repository> _logger;

        public $procedure.CSName$Repository(
            IDbContextFactory<$contextNameSpace$> contextFactory,
            ILogger<$procedure.CSName$Repository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("Initializing $procedure.CSName$Repository");
        \}

        public async Task<List<$procedure.CSName$Dto>> ExecuteAsync($procedure.CSName$InputDto parameters)
        {
            if (parameters == null)
            {
                _logger.LogWarning("Attempt to execute $procedure.CSName$ with null parameters");
                throw new ArgumentNullException(nameof(parameters));
            \}

            _logger.LogDebug("Executing $procedure.CSName$ stored procedure with parameters: " + parameters.ToString());

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                return await context.Procedures.$procedure.CSName$Async(parameters);
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