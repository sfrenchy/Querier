using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Models;
using System.Linq.Dynamic.Core;
using $model.RootNamespace$.Contexts;
using $model.RootNamespace$.Entities;

namespace $model.RootNamespace$.Repositories;

/// <summary>
/// Repository implementation for $model.Entity.Name$ entity
/// </summary>
public class $model.Entity.Name$Repository : I$model.Entity.Name$Repository
{
    private readonly IDbContextFactory<$model.RootNamespace$DbContext> _contextFactory;
    private readonly ILogger<$model.Entity.Name$Repository> _logger;

    public $model.Entity.Name$Repository(
        IDbContextFactory<$model.RootNamespace$DbContext> contextFactory,
        ILogger<$model.Entity.Name$Repository> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("Initializing $model.Entity.Name$Repository");
    \}

    public async Task<DataPagedResult<$model.Entity.Name$>> GetAllAsync(DataRequestParametersDto? parameters = null)
    {
        _logger.LogDebug("Getting all $model.Entity.PluralName$ with parameters: " + parameters.ToString());

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.$model.Entity.PluralName$.AsQueryable();

            // Apply search filters
            if (!string.IsNullOrEmpty(parameters?.GlobalSearch))
            {
                _logger.LogDebug("Applying global search filter: " + parameters.GlobalSearch);
                var searchTerm = parameters.GlobalSearch.ToLower();
                bool isInt = int.TryParse(searchTerm, out int searchInt);
                bool isDecimal = decimal.TryParse(searchTerm, out decimal searchDecimal);
                bool isDate = DateTime.TryParse(searchTerm, out DateTime searchDate);
                bool isDouble = double.TryParse(searchTerm, out double searchDouble);
                bool isFloat = float.TryParse(searchTerm, out float searchFloat);
                bool isLong = long.TryParse(searchTerm, out long searchLong);
                bool isShort = short.TryParse(searchTerm, out short searchShort);
                bool isByte = byte.TryParse(searchTerm, out byte searchByte);
                bool isBool = bool.TryParse(searchTerm, out bool searchBool);
                bool isGuid = Guid.TryParse(searchTerm, out Guid searchGuid);
                bool isTimeSpan = TimeSpan.TryParse(searchTerm, out TimeSpan searchTimeSpan);

                query = query.Where(e =>
                    $model.Entity.Properties: {property|
                    $if(!property.IsArray && property.IsString)$
                    e.$property.Name$.ToString().ToLower().Contains(searchTerm) ||
                    $endif$
                    $if(!property.IsArray && property.IsInt)$
                    (isInt && e.$property.Name$ == ($property.CSType$)searchInt) ||
                    $endif$
                    $if(!property.IsArray && property.IsDateTime)$
                    (isDate && e.$property.Name$ == searchDate) ||
                    $endif$
                    $if(!property.IsArray && property.IsDecimal)$
                    (isDecimal && e.$property.Name$ == ($property.CSType$)searchDecimal) ||
                    $endif$
                    $if(!property.IsArray && property.IsDouble)$
                    (isDouble && e.$property.Name$ == ($property.CSType$)searchDouble) ||
                    $endif$
                    $if(!property.IsArray && property.IsFloat)$
                    (isFloat && e.$property.Name$ == ($property.CSType$)searchFloat) ||
                    $endif$
                    $if(!property.IsArray && property.IsLong)$
                    (isLong && e.$property.Name$ == ($property.CSType$)searchLong) ||
                    $endif$
                    $if(!property.IsArray && property.IsShort)$
                    (isShort && e.$property.Name$ == ($property.CSType$)searchShort) ||
                    $endif$
                    $if(!property.IsArray && property.IsByte)$
                    (isByte && e.$property.Name$ == ($property.CSType$)searchByte) ||
                    $endif$
                    $if(!property.IsArray && property.IsBool)$
                    (isBool && e.$property.Name$ == searchBool) ||
                    $endif$
                    $if(!property.IsArray && property.IsGuid)$
                    (isGuid && e.$property.Name$ == searchGuid) ||
                    $endif$
                    $if(!property.IsArray && property.IsTimeSpan)$
                    (isTimeSpan && e.$property.Name$ == searchTimeSpan) ||
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
                    switch (columnName)
                    {
                    $model.Entity.Properties: {property|
                        case "$property.Name$":
                            
                            $if(!property.IsArray && property.IsString)$
                            query = query.Where(e => searchTerms.Any(term => e.$property.Name$.ToString().ToLower().Contains(term)));
                            $endif$

                            $if(!property.IsArray && property.IsInt)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => int.TryParse(term, out _))
                                .Select(term => ($property.CSType$)int.Parse(term))
                                .ToList();
                            query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsDateTime)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => DateTime.TryParse(term, out _))
                                .Select(term => (DateTime?)DateTime.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsDecimal)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => decimal.TryParse(term, out _))
                                .Select(term => ($property.CSType$)decimal.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsDouble)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => double.TryParse(term, out _))
                                .Select(term => ($property.CSType$)double.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsFloat)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => float.TryParse(term, out _))
                                .Select(term => ($property.CSType$)float.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsLong)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => long.TryParse(term, out _))
                                .Select(term => ($property.CSType$)long.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsShort)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => short.TryParse(term, out _))
                                .Select(term => ($property.CSType$)short.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsByte)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => byte.TryParse(term, out _))
                                .Select(term => ($property.CSType$)byte.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsBool)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => bool.TryParse(term, out _))
                                .Select(term => (bool?)bool.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsGuid)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => Guid.TryParse(term, out _))
                                .Select(term => (Guid?)Guid.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$

                            $if(!property.IsArray && property.IsTimeSpan)$
                            var $property.Name$Terms = searchTerms
                            .Where(term => TimeSpan.TryParse(term, out _))
                                .Select(term => (TimeSpan?)TimeSpan.Parse(term))
                                .ToList();
                                query = query.Where(e => $property.Name$Terms.Contains(e.$property.Name$));
                            $endif$
                                
                            break;
                        }$
                        default:
                            _logger.LogWarning("Unknown column name: " + columnName);
                            break;
                    \}
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
                
            _logger.LogInformation("Successfully retrieved " + result.Count().ToString() + " $model.Entity.Name$ records");
            return new DataPagedResult<$model.Entity.Name$>(result, totalCount, parameters);
        \}
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving $model.Entity.PluralName$");
            throw;
        \}
    \}

    $if (model.Entity.IsTableEntity)$
        $if (model.Entity.HasKey)$
            public async Task<$model.Entity.Name$?> GetByIdAsync($model.Entity.MethodSignatureParameter$)
            {
                _logger.LogDebug(\$"Getting $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");

                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    var entity = await context.$model.Entity.PluralName$.AsNoTracking().FirstOrDefaultAsync($model.Entity.LinqEntityFilter$);

                    if (entity == null)
                    {
                        _logger.LogInformation(\$"$model.Entity.Name$ with id $model.Entity.StringConcatParameters$ not found");
                        return null;
                    \}

                    _logger.LogDebug(\$"Successfully retrieved $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
                    return entity;
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, \$"Error getting $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
                    throw;
                \}
            \}
    
            public async Task<$model.Entity.Name$> CreateAsync($model.Entity.Name$ entity)
            {
                if (entity == null)
                {
                    _logger.LogWarning("Attempt to create $model.Entity.Name$ with null entity");
                    throw new ArgumentNullException(nameof(entity));
                \}
    
                _logger.LogDebug("Creating new $model.Entity.Name$ entity");
    
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    
                    _logger.LogDebug("Adding entity to context");
                    context.$model.Entity.PluralName$.Add(entity);
                    
                    _logger.LogDebug("Saving changes to database");
                    await context.SaveChangesAsync();
    
                    _logger.LogInformation("Successfully created new $model.Entity.Name$");
                    return entity;
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating new $model.Entity.Name$ entity");
                    throw;
                \}
            \}
    
            public async Task<$model.Entity.Name$?> UpdateAsync($model.Entity.MethodSignatureParameter$, $model.Entity.Name$ entity)
            {
                if (entity == null)
                {
                    _logger.LogWarning("Attempt to update $model.Entity.Name$ with null entity");
                    throw new ArgumentNullException(nameof(entity));
                \}
    
                _logger.LogDebug(\$"Updating $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
    
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    
                    var existingEntity = await GetByIdAsync($model.Entity.KeyParameterLine$);
                    if (existingEntity == null)
                    {
                        _logger.LogInformation(\$"$model.Entity.Name$ with id $model.Entity.StringConcatParameters$ not found for update");
                        return null;
                    \}
    
                    _logger.LogDebug("Updating entity in context");
                    context.Entry(existingEntity).CurrentValues.SetValues(entity);
                    
                    _logger.LogDebug("Saving changes to database");
                    await context.SaveChangesAsync();
    
                    _logger.LogInformation(\$"Successfully updated $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
                    return existingEntity;
                \}
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, \$"Concurrency conflict while updating $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
                    throw;
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, \$"Error updating $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
                    throw;
                \}
            \}
    
            public async Task<bool> DeleteAsync($model.Entity.MethodSignatureParameter$)
            {
                _logger.LogDebug(\$"Deleting $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
    
                try
                {
                    await using var context = await _contextFactory.CreateDbContextAsync();
                    
                    var entity = await context.$model.Entity.PluralName$.FindAsync($model.Entity.KeyParameterLine$);
                    if (entity == null)
                    {
                        _logger.LogInformation(\$"$model.Entity.Name$ with id $model.Entity.StringConcatParameters$ not found for deletion");
                        return false;
                    \}
    
                    _logger.LogDebug("Removing entity from context");
                    context.$model.Entity.PluralName$.Remove(entity);
                    
                    _logger.LogDebug("Saving changes to database");
                    await context.SaveChangesAsync();
    
                    _logger.LogInformation(\$"Successfully deleted $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
                    return true;
                \}
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, \$"Concurrency conflict while deleting $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
                    throw;
                \}
                catch (Exception ex)
                {
                    _logger.LogError(ex, \$"Error deleting $model.Entity.Name$ with id $model.Entity.StringConcatParameters$");
                    throw;
                \}
            \}
        $endif$
    $endif$
    public async Task<List<string>> GetUniqueValuesForColumnAsync(string columnName)
    {
        _logger.LogDebug("Getting unique values for column " + columnName + " in $model.Entity.Name$");

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
                $model.Entity.Properties: {property|
                case "$property.Name$":
                {
                    _logger.LogDebug("Fetching unique values for $property.Name$");
                    result.AddRange(await context.$model.Entity.PluralName$
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