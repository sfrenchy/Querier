using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Domain.Common.Models;
using Northwind.Entities;

namespace $model.RootNamespace$.Repositories;

/// <summary>
/// Repository interface for $model.Entity.Name$ entity
/// </summary>
public interface I$model.Entity.Name$Repository
{
    /// <summary>
    /// Gets a paged list of $model.Entity.PluralName$
    /// </summary>
    Task<DataPagedResult<$model.Entity.Name$>> GetAllAsync(DataRequestParametersDto? parameters = null);

    $if (model.Entity.IsTableEntity)$
        /// <summary>
        /// Gets a $model.Entity.Name$ by its identifier
        /// </summary>
        Task<$model.Entity.Name$?> GetByIdAsync($model.Entity.MethodSignatureParameter$);
    
        /// <summary>
        /// Creates a new $model.Entity.Name$
        /// </summary>
        Task<$model.Entity.Name$> CreateAsync($model.Entity.Name$ entity);

        /// <summary>
        /// Updates an existing $model.Entity.Name$
        /// </summary>
        Task<$model.Entity.Name$?> UpdateAsync($model.Entity.MethodSignatureParameter$, $model.Entity.Name$ entity);

        /// <summary>
        /// Deletes a $model.Entity.Name$ by its identifier
        /// </summary>
        Task<bool> DeleteAsync($model.Entity.MethodSignatureParameter$);
    $endif$
        
    /// <summary>
    /// Gets unique values for a specific column
    /// </summary>
    Task<List<string>> GetUniqueValuesForColumnAsync(string columnName);
\}