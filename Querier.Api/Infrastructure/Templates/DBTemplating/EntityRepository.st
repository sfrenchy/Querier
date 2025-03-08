using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Models;
using System.Linq.Dynamic.Core;
using $rootNamespace$.Contexts;
using $rootNamespace$.Models;

namespace $rootNamespace$.Repositories
{
    $entityList: {entity|
    #region $entity.Name$
    /// <summary>
    /// Repository interface for $entity.Name$ entity
    /// </summary>
    public interface I$entity.Name$Repository
    {
        /// <summary>
        /// Gets a paged list of $entity.PluralName$
        /// </summary>
        Task<DataPagedResult<$entity.Name$>> GetAllAsync(DataRequestParametersDto? parameters = null);

        /// <summary>
        /// Gets a $entity.Name$ by its identifier
        /// </summary>
        Task<$entity.Name$?> GetByIdAsync($entity.KeyType$ id);
        
        $if (entity.IsTableEntity)$
        /// <summary>
        /// Creates a new $entity.Name$
        /// </summary>
        Task<$entity.Name$> CreateAsync($entity.Name$ entity);

        /// <summary>
        /// Updates an existing $entity.Name$
        /// </summary>
        Task<$entity.Name$?> UpdateAsync($entity.Name$ entity);

        /// <summary>
        /// Deletes a $entity.Name$ by its identifier
        /// </summary>
        Task<bool> DeleteAsync($entity.KeyType$ id);
        $endif$
        
        /// <summary>
        /// Gets unique values for a specific column
        /// </summary>
        Task<List<string>> GetUniqueValuesForColumnAsync(string columnName);
    \}

    /// <summary>
    /// Repository implementation for $entity.Name$ entity
    /// </summary>
    public class $entity.Name$Repository : I$entity.Name$Repository
    {
        private readonly IDbContextFactory<$contextName$> _contextFactory;
        private readonly ILogger<$entity.Name$Repository> _logger;

        public $entity.Name$Repository(
            IDbContextFactory<$contextName$> contextFactory,
            ILogger<$entity.Name$Repository> logger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("Initializing $entity.Name$Repository");
        \}

        public async Task<DataPagedResult<$entity.Name$>> GetAllAsync(DataRequestParametersDto? parameters = null)
        {
            _logger.LogDebug("Getting all $entity.PluralName$ with parameters: " + parameters.ToString());

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var query = context.$entity.PluralName$.AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(parameters?.GlobalSearch))
                {
                    _logger.LogDebug("Applying global search filter: " + parameters.GlobalSearch);
                    var searchTerm = parameters.GlobalSearch.ToLower();
                    query = query.Where(e =>
                        $entity.Properties: {property|
                        $if(property.IsLinqToSqlSupportedType)$
                        e.$property.Name$.ToString().ToLower().Contains(searchTerm) ||
                        $endif$
                        }$
                        false);
                \}

                // Apply column-specific searches
                if (parameters?.ColumnSearches?.Any() == true)
                {
                    _logger.LogDebug("Applying column-specific searches");
                    var groupedSearches = parameters.ColumnSearches
                        .GroupBy(cs => cs.Column)
                        .ToDictionary(g => g.Key, g => g.Select(cs => cs.Value.ToLower()).ToList());

                    foreach (var columnSearch in groupedSearches)
                    {
                        var columnName = columnSearch.Key;
                        var searchTerms = columnSearch.Value;
                        _logger.LogDebug("Applying search for column " + columnName + " with terms: " + searchTerms);

                        query = columnName switch
                        {
                            $entity.Properties: {property|
                            "$property.Name$" => query.Where(e => searchTerms.Any(term => 
                                e.$property.Name$.ToString().ToLower().Contains(term))),
                            }$
                            _ => query
                        \};
                    \}
                \}

                var totalCount = await query.CountAsync();
                _logger.LogDebug("Total count before sorting and pagination: " + totalCount.ToString());

                // Apply sorting
                if (parameters?.OrderBy?.Any() == true)
                {
                    _logger.LogDebug("Applying sorting");
                    foreach (var orderBy in parameters.OrderBy)
                    {
                        string direction = orderBy.IsDescending ? "descending" : "ascending";
                        string column = orderBy.Column;
                        var sortExpression = column + " " + direction;
                        _logger.LogDebug("Sorting by " + column + " " + direction);
                        query = query.OrderBy(sortExpression);
                    \}
                \}

                // Apply pagination
                var pageSize = parameters?.PageSize ?? totalCount;
                var pageNumber = parameters?.PageNumber ?? 0;
                var skip = pageNumber != 0 ? (pageNumber - 1) * pageSize : 0;

                _logger.LogDebug("Applying pagination: Skip=" + skip.ToString() + ", Take=" + pageSize.ToString());
                
                var data = query.AsNoTracking();

                if (pageSize != 0)
                    data = data.Skip(skip).Take(pageSize);

                var result = await data.ToListAsync();
                
                _logger.LogInformation("Successfully retrieved " + result.Count().ToString() + " $entity.Name$ records");
                return new DataPagedResult<$entity.Name$>(result, totalCount, parameters);
            \}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving $entity.PluralName$");
                throw;
            \}
        \}

        public async Task<$entity.Name$?> GetByIdAsync($entity.KeyType$ id)
        {
            _logger.LogDebug("Getting $entity.Name$ with id " + id.ToString());

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var entity = await context.$entity.PluralName$.AsNoTracking().FirstOrDefaultAsync(e => e.$entity.KeyName$ == id);

                if (entity == null)
                {
                    _logger.LogInformation("$entity.Name$ with id " + id.ToString() + " not found");
                    return null;
                \}

                _logger.LogDebug("Successfully retrieved $entity.Name$ with id " + id.ToString());
                return entity;
            \}
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting $entity.Name$ with id " + id.ToString());
                throw;
            \}
        \}
        $if (entity.IsTableEntity)$
            public async Task<$entity.Name$> CreateAsync($entity.Name$ entity)
            {
                if (entity == null)
                {
                    _logger.LogWarning("Attempt to create $entity.Name$ with null entity");
                    throw new ArgumentNullException(nameof(entity));
                \}
    
                _logger.LogDebug("Creating new $entity.Name$ entity");
    
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    
                    _logger.LogDebug("Adding entity to context");
                    context.$entity.PluralName$.Add(entity);
                    
                    _logger.LogDebug("Saving changes to database");
                    await context.SaveChangesAsync();
    
                    _logger.LogInformation("Successfully created new $entity.Name$ with id " + entity.$entity.KeyName$.ToString());
                    return entity;
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating new $entity.Name$ entity");
                    throw;
                \}
            \}
    
            public async Task<$entity.Name$?> UpdateAsync($entity.Name$ entity)
            {
                if (entity == null)
                {
                    _logger.LogWarning("Attempt to update $entity.Name$ with null entity");
                    throw new ArgumentNullException(nameof(entity));
                \}
    
                _logger.LogDebug("Updating $entity.Name$ with id " + entity.$entity.KeyName$.ToString());
    
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    
                    var existingEntity = await context.$entity.PluralName$.FindAsync(entity.$entity.KeyName$);
                    if (existingEntity == null)
                    {
                        _logger.LogInformation("$entity.Name$ with id " + entity.$entity.KeyName$.ToString() + " not found for update");
                        return null;
                    \}
    
                    _logger.LogDebug("Updating entity in context");
                    context.Entry(existingEntity).CurrentValues.SetValues(entity);
                    
                    _logger.LogDebug("Saving changes to database");
                    await context.SaveChangesAsync();
    
                    _logger.LogInformation("Successfully updated $entity.Name$ with id " + entity.$entity.KeyName$.ToString());
                    return existingEntity;
                \}
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict while updating $entity.Name$ with id " + entity.$entity.KeyName$.ToString());
                    throw;
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating $entity.Name$ with id " + entity.$entity.KeyName$.ToString());
                    throw;
                \}
            \}
    
            public async Task<bool> DeleteAsync($entity.KeyType$ id)
            {
                _logger.LogDebug("Deleting $entity.Name$ with id " + id.ToString());
    
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    
                    var entity = await context.$entity.PluralName$.FindAsync(id);
                    if (entity == null)
                    {
                        _logger.LogInformation("$entity.Name$ with id " + id.ToString() + " not found for deletion");
                        return false;
                    \}
    
                    _logger.LogDebug("Removing entity from context");
                    context.$entity.PluralName$.Remove(entity);
                    
                    _logger.LogDebug("Saving changes to database");
                    await context.SaveChangesAsync();
    
                    _logger.LogInformation("Successfully deleted $entity.Name$ with id " + id.ToString());
                    return true;
                \}
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency conflict while deleting $entity.Name$ with id " + id.ToString());
                    throw;
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting $entity.Name$ with id " + id.ToString());
                    throw;
                \}
            \}
        $endif$
        public async Task<List<string>> GetUniqueValuesForColumnAsync(string columnName)
        {
            _logger.LogDebug("Getting unique values for column " + columnName + " in $entity.Name$");

            if (string.IsNullOrEmpty(columnName))
            {
                _logger.LogWarning("Column name is null or empty");
                throw new ArgumentException("Column name cannot be null or empty", nameof(columnName));
            \}

            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var result = new List<string>();

                switch (columnName)
                {
                    $entity.Properties: {property|
                    case "$property.Name$":
                    {
                        _logger.LogDebug("Fetching unique values for $property.Name$");
                        result.AddRange(await context.$entity.PluralName$
                            .AsNoTracking()
                            .Select(e => e.$property.Name$.ToString())
                            .Distinct()
                            .OrderBy(value => value)
                            .ToListAsync());
                        break;
                    \}
                    }$
                    default:
                        _logger.LogWarning("Unknown column name: " + columnName);
                        throw new ArgumentException("Column " + columnName + " does not exist");
                \}

                _logger.LogDebug("Found " + result.Count.ToString() + " unique values for column " + columnName);
                return result;
            \}
            catch (Exception ex) when (ex is not ArgumentException)
            {
                _logger.LogError(ex, "Error getting unique values for column " + columnName);
                throw;
            \}
        \}
    \}
    #endregion
    }$
\} 