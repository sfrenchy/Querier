using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using $nameSpace$.Repositories;
using $nameSpace$.Exceptions;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Common.Attributes;
using Querier.Api.Infrastructure.Database.Services.Base;
using Querier.Api.Infrastructure.Database.Parameters;

namespace $nameSpace$.Services
{
    $procedureList: {procedure|
    #region $procedure.Name$
    /// <summary>
    /// Service interface for $procedure.CSName$ stored procedure
    /// </summary>
    public interface I$procedure.CSName$Service
    {
        /// <summary>
        /// Executes the $procedure.CSName$ stored procedure
        /// </summary>
        /// <param name="parameters">The parameters for the stored procedure</param>
        /// <returns>A list of results from the stored procedure execution</returns>
        Task<List<$procedure.CSName$Dto>> ExecuteAsync($procedure.CSName$InputDto parameters);
    \}

    [DynamicContextProcedure(ContextName="$nameSpace$", ServiceName="I$procedure.CSName$Service")]
    public class $procedure.CSName$Service : DynamicContextServiceBase, I$procedure.CSName$Service
    {
        private readonly I$procedure.CSName$Repository _repository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<$procedure.CSName$Service> _logger;
        private static readonly DistributedCacheEntryOptions _cacheOptions = new() {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        \};

        public $procedure.CSName$Service(
            I$procedure.CSName$Repository repository,
            IDistributedCache cache,
            ILogger<$procedure.CSName$Service> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _logger.LogDebug("Initializing $procedure.CSName$Service");
        \}

        private async Task<T> ExecuteCacheOperationAsync<T>(string operation, string cacheKey, Func<Task<T>> databaseOperation) {
            try {
                var cachedData = await _cache.GetAsync(cacheKey);
                if (cachedData != null) {
                    _logger.LogDebug("Cache hit for key: " + cacheKey);
                    using var stream = new MemoryStream(cachedData);
                    using var reader = new StreamReader(stream);
                    return JsonConvert.DeserializeObject<T>(await reader.ReadToEndAsync()) 
                        ?? throw new CacheOperationException("Deserialize", "Failed to deserialize cached data");
                \}

                _logger.LogDebug("Cache miss for key: " + cacheKey);
                var startTime = DateTime.UtcNow;

                var result = await databaseOperation();

                var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogDebug("Operation executed in " + executionTime.ToString() + "ms");

                if (result != null) {
                    try {
                        var serializedData = JsonConvert.SerializeObject(result);
                        await _cache.SetAsync(
                            cacheKey,
                            System.Text.Encoding.UTF8.GetBytes(serializedData),
                            _cacheOptions
                        );
                    \}
                    catch (Exception ex) {
                        _logger.LogWarning(ex, "Failed to cache result for key: " + cacheKey);
                    \}
                \}

                return result;
            \}
            catch (Exception ex) when (ex is not DynamicContextException) {
                throw new CacheOperationException(operation, "Cache operation failed", ex);
            \}
        \}

        public async Task<List<$procedure.CSName$Dto>> ExecuteAsync($procedure.CSName$InputDto parameters) {
            if (parameters == null) {
                throw new EntityValidationException("$procedure.CSName$", "Parameters cannot be null");
            \}

            // Validate required parameters
            $procedure.Parameters: {param|
            $if(!param.IsOutput && !param.IsNullable)$
            if (parameters.$param.CSName$ == null) {
                throw new EntityValidationException("$procedure.CSName$", "Parameter $param.Name$ is required");
            \}
            $endif$
            }$

            try {
                var cacheKey = "$procedure.CSName$_" + Utils.ComputeMd5Hash(JsonConvert.SerializeObject(parameters));
                return await ExecuteCacheOperationAsync("Execute", cacheKey, async () => {
                    try {
                        var results = await _repository.ExecuteAsync($if(procedure.HasParameters)$parameters$endif$);
                        return results ?? new List<$procedure.CSName$Dto>();
                    \}
                    catch (Exception ex) {
                        throw new StoredProcedureException(
                            "$procedure.CSName$",
                            "Failed to execute stored procedure",
                            ex
                        );
                    \}
                \});
            \}
            catch (Exception ex) when (ex is not DynamicContextException) {
                throw new StoredProcedureException(
                    "$procedure.CSName$",
                    "An unexpected error occurred while executing the stored procedure",
                    ex
                );
            \}
        \}
    \}
    #endregion
    }$
\}