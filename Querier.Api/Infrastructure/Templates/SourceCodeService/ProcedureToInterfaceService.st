using Querier.Api.Domain.Common.Models;
using System.Threading.Tasks;
using $model.RootNamespace$.Repositories;
using $model.RootNamespace$.Dtos.Procedure;

namespace $model.RootNamespace$.Services.Procedure;

/// <summary>
/// Service interface for $procedure.CSName$ stored procedure
/// </summary>
public interface I$model.CSName$Service
{
    /// <summary>
    /// Executes the $model.CSName$ stored procedure
    /// </summary>
    /// <param name="parameters">The parameters for the stored procedure</param>
    /// <returns>A list of results from the stored procedure execution</returns>
    $if(model.HasParameters)$
        Task$if(model.HasOutput)$<DataPagedResult<$model.CSName$OutputDto>>$endif$ ExecuteAsync($model.CSName$InputDto parameters);
    $else$
        Task$if(model.HasOutput)$<DataPagedResult<$model.CSName$OutputDto>>$endif$ ExecuteAsync();
    $endif$
\}