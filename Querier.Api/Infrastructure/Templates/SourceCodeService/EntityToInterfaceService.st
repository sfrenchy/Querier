using System.Collections.Generic;
using System.Threading.Tasks;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Common.Models;
using $model.RootNamespace$.Dtos;
using $model.RootNamespace$.Repositories;
using $model.RootNamespace$.Entities;

namespace $model.RootNamespace$.Services;

/// <summary>
/// Service interface for managing $model.Entity.Name$ entities
/// </summary>
public interface I$model.Entity.Name$Service : IDynamicContextEntityService
{
    /// <summary>
    /// Retrieves a paged list of $model.Entity.Name$ entities
    /// </summary>
    /// <param name="parameters">Optional parameters for pagination, sorting and filtering</param>
    /// <returns>A paged result containing $model.Entity.Name$ DTOs</returns>
    Task<DataPagedResult<Get$model.Entity.Name$Dto>> GetAllAsync(DataRequestParametersDto? parameters = null);
    
    $if (model.Entity.IsTableEntity)$
        /// <summary>
        /// Retrieves a specific $model.Entity.Name$ by its identifier
        /// </summary>
        /// <param name="id">The identifier of the $model.Entity.Name$</param>
        /// <returns>The $model.Entity.Name$ DTO if found, null otherwise</returns>
        Task<Get$model.Entity.Name$Dto?> GetByIdAsync($model.Entity.MethodSignatureParameter$);
    
        /// <summary>
        /// Creates a new $model.Entity.Name$ entity
        /// </summary>
        /// <param name="createDto">The DTO containing the creation data</param>
        /// <returns>The created $model.Entity.Name$ DTO</returns>
        Task<Get$model.Entity.Name$Dto> CreateAsync(Create$model.Entity.Name$Dto createDto);

        /// <summary>
        /// Updates an existing $model.Entity.Name$ entity
        /// </summary>
        /// <param name="id">The identifier of the entity to update</param>
        /// <param name="updateDto">The DTO containing the update data</param>
        /// <returns>The updated $model.Entity.Name$ DTO if found, null otherwise</returns>
        Task<Get$model.Entity.Name$Dto?> UpdateAsync($model.Entity.MethodSignatureParameter$, Update$model.Entity.Name$Dto updateDto);

        /// <summary>
        /// Deletes a specific $model.Entity.Name$ entity
        /// </summary>
        /// <param name="id">The identifier of the entity to delete</param>
        /// <returns>True if the entity was deleted, false if not found</returns>
        Task<bool> DeleteAsync($model.Entity.MethodSignatureParameter$);
    $endif$
        
    /// <summary>
    /// Retrieves unique values for a specific column
    /// </summary>
    /// <param name="column">The name of the column</param>
    /// <returns>List of unique values as strings</returns>
    Task<List<string>> ColumnUniqueValuesAsync(string column);
\}
