using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Querier.Api.Infrastructure.Database.Parameters;
using Querier.Api.Infrastructure.Database.Extensions;
using $model.RootNamespace$.Dtos.Procedure;
using $model.RootNamespace$.Contexts;

namespace $model.RootNamespace$.Repositories.Procedure;

/// <summary>
/// Repository implementation for $procedure.CSName$ stored procedure
/// </summary>
public class $model.CSName$Repository : I$model.CSName$Repository
{
    private readonly IDbContextFactory<$model.RootNamespace$DbContext> _contextFactory;
    private readonly ILogger<$model.CSName$Repository> _logger;

    public $model.CSName$Repository(
        IDbContextFactory<$model.RootNamespace$DbContext> contextFactory,
        ILogger<$model.CSName$Repository> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    \}

    $if(model.HasParameters)$
    public async Task<List<$model.CSName$OutputDto>> ExecuteAsync($model.CSName$InputDto parameters)
    {
        if (parameters == null)
        {
            _logger.LogWarning("Attempt to execute $model.CSName$ with null parameters");
            throw new ArgumentNullException(nameof(parameters));
        \}

        _logger.LogDebug("Executing $model.CSName$ stored procedure with parameters: " + parameters.ToString());
    $else$
    public async Task<List<$model.CSName$OutputDto>> ExecuteAsync()
    {
        _logger.LogDebug("Executing $model.CSName$ stored procedure");
    $endif$

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            $if(model.HasParameters)$
                return await $model.CSName$Async(parameters);
            $else$
                return await $model.CSName$Async();
            $endif$
        \}
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing $model.CSName$ stored procedure");
            throw;
        \}
    \}

    public virtual async Task<List<$model.CSName$OutputDto>> $model.CSName$Async($if(model.HasParameters)$$model.CSName$InputDto inputDto, $endif$OutputParameter<int> returnValue = null, CancellationToken cancellationToken = default)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var parameterreturnValue = new SqlParameter
        {
            ParameterName = "returnValue",
            Direction = System.Data.ParameterDirection.Output,
            SqlDbType = System.Data.SqlDbType.Int,
        \};

        var sqlParameters = new []
        {
            $if(model.HasParameters)$
            $model.Parameters: {parameter|
                new SqlParameter
                {
                    ParameterName = "$parameter.Name$",
                    Value = inputDto.$parameter.CSName$ ?? Convert.DBNull,
                    SqlDbType = $parameter.SqlParameterType$
                \},
            }$
            $endif$
            parameterreturnValue
        \};

        $if (model.HasOutput)$
            var _ = await context.SqlQueryAsync<$model.CSName$OutputDto>("EXEC @returnValue = [dbo].[$model.Name$] $model.InlineParameters$", sqlParameters, cancellationToken);
        $else$
            await context.SqlQueryAsync<object>("EXEC @returnValue = [dbo].[$model.Name$] $model.InlineParameters$", sqlParameters, cancellationToken);
        $endif$

        returnValue?.SetValue(parameterreturnValue.Value);

        $if (model.HasOutput)$
        return _;
        $endif$
    \}
\}