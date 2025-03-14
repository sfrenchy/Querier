using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Common.Attributes;
using Querier.Api.Infrastructure.Base;
using Querier.Api.Infrastructure.Database.Parameters;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Application.DTOs;
using Querier.Api.Infrastructure.Base.Exceptions;
using $model.RootNamespace$.Repositories.Procedure;
using $model.RootNamespace$.Services.Procedure;
using $model.RootNamespace$.Dtos.Procedure;

namespace $model.RootNamespace$.Services;

[DynamicContextProcedure(ContextName="$model.RootNamespace$.Contexts.$model.RootNamespace$DbContext", ServiceName="I$model.CSName$Service")]
public class $model.CSName$Service : DynamicContextServiceBase<$model.CSName$Service>, I$model.CSName$Service
{
    private readonly I$model.CSName$Repository _repository;
    private readonly IDistributedCache _cache;
    private readonly ILogger<$model.CSName$Service> _logger;
    private static readonly DistributedCacheEntryOptions _cacheOptions = new() {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    \};

    public $model.CSName$Service(
        I$model.CSName$Repository repository,
        IDistributedCache cache,
        ILogger<$model.CSName$Service> logger) : base(cache, logger)
    {
        CACHE_VERSION_KEY = "$model.Entity.Name$_CacheVersion";
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
        _logger.LogDebug("Initializing $model.CSName$Service");
    \}
    $if(model.HasParameters)$
    public async Task<DataPagedResult<$model.CSName$OutputDto>> ExecuteAsync($model.CSName$InputDto parameters) {
    if (parameters == null) {
            throw new EntityValidationException("$model.RootNamespace$", "$model.CSName$", "Parameters cannot be null");
        \}
    $else$
    public async Task<DataPagedResult<$model.CSName$OutputDto>> ExecuteAsync() {
    $endif$
        

        // Validate required parameters
        $model.Parameters: {param|
        $if(!param.IsOutput && !param.IsNullable)$
        if (parameters.$param.CSName$ == null) {
            throw new EntityValidationException("$model.RootNamespace$", "$model.CSName$", "Parameter $param.Name$ is required");
        \}
        $endif$
        }$

        try {   
            $if(model.HasParameters)$
                var cacheKey = "$model.CSName$_" + Utils.ComputeMd5Hash(JsonConvert.SerializeObject(parameters));
            $else$
                var cacheKey = "$model.CSName$";
            $endif$
            return await ExecuteCacheOperationAsync("$model.RootNamespace$", "Execute", cacheKey, async () => {
                try {
                    var results = await _repository.ExecuteAsync($if(model.HasParameters)$parameters$endif$);
                    return new DataPagedResult<$model.CSName$OutputDto>(
                        results,
                        results.Count(),
                        new DataRequestParametersDto(),
                        new List<ForeignKeyDataDto>()
                    );
                \}
                catch (Exception ex) {
                    throw new StoredProcedureException(
                        "$model.RootNamespace$",
                        "$model.CSName$",
                        "Failed to execute stored procedure",
                        ex
                    );
                \}
            \});
        \}
        catch (Exception ex) when (ex is not DynamicContextException) {
            throw new StoredProcedureException(
                "$model.RootNamespace$",
                "$model.CSName$",
                "An unexpected error occurred while executing the stored procedure",
                ex
            );
        \}
    \}
\}