using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Infrastructure.Database.Parameters;
using $model.RootNamespace$.Dtos.Procedure;

namespace $model.RootNamespace$.Repositories.Procedure;

/// <summary>
/// Repository interface for $procedure.CSName$ stored procedure
/// </summary>
public interface I$model.CSName$Repository
{
    /// <summary>
    /// Executes the $model.CSName$ stored procedure
    /// </summary>
    $if(model.HasParameters)$
        Task<List<$model.CSName$OutputDto>> ExecuteAsync($model.CSName$InputDto parameters);
    $else$
        Task<List<$model.CSName$OutputDto>> ExecuteAsync();
    $endif$
\}