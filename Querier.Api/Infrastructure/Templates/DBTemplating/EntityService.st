using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Domain.Common.Attributes;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Common.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.IO;
using Querier.Api.Application.DTOs;
using $rootNamespace$.Exceptions;
using $rootNamespace$.DTOs;
using $rootNamespace$.Repositories;
using $rootNamespace$.Models;

namespace $rootNamespace$.Services
{
    $entityList: {entity|
    #region $entity.Name$
    /// <summary>
    /// Service interface for managing $entity.Name$ entities
    /// </summary>
    public interface I$entity.Name$Service : IDynamicContextEntityService
    {
        /// <summary>
        /// Retrieves a paged list of $entity.Name$ entities
        /// </summary>
        /// <param name="parameters">Optional parameters for pagination, sorting and filtering</param>
        /// <returns>A paged result containing $entity.Name$ DTOs</returns>
        Task<DataPagedResult<$entity.Name$Dto>> GetAllAsync(DataRequestParametersDto? parameters = null);

        /// <summary>
        /// Retrieves a specific $entity.Name$ by its identifier
        /// </summary>
        /// <param name="id">The identifier of the $entity.Name$</param>
        /// <returns>The $entity.Name$ DTO if found, null otherwise</returns>
        Task<$entity.Name$Dto?> GetByIdAsync($entity.KeyType$ id);

        /// <summary>
        /// Creates a new $entity.Name$ entity
        /// </summary>
        /// <param name="createDto">The DTO containing the creation data</param>
        /// <returns>The created $entity.Name$ DTO</returns>
        Task<$entity.Name$Dto> CreateAsync(Create$entity.Name$Dto createDto);

        /// <summary>
        /// Updates an existing $entity.Name$ entity
        /// </summary>
        /// <param name="id">The identifier of the entity to update</param>
        /// <param name="updateDto">The DTO containing the update data</param>
        /// <returns>The updated $entity.Name$ DTO if found, null otherwise</returns>
        Task<$entity.Name$Dto?> UpdateAsync($entity.KeyType$ id, Update$entity.Name$Dto updateDto);

        /// <summary>
        /// Deletes a specific $entity.Name$ entity
        /// </summary>
        /// <param name="id">The identifier of the entity to delete</param>
        /// <returns>True if the entity was deleted, false if not found</returns>
        Task<bool> DeleteAsync($entity.KeyType$ id);

        /// <summary>
        /// Retrieves unique values for a specific column
        /// </summary>
        /// <param name="column">The name of the column</param>
        /// <returns>List of unique values as strings</returns>
        Task<List<string>> ColumnUniqueValuesAsync(string column);
    \}

    [DynamicContextEntity(ContextName="$nameSpace$", ServiceName="I$entity.Name$Service")]
    public class $entity.Name$Service : I$entity.Name$Service
    {
        private readonly I$entity.Name$Repository _repository;
        $entity.ForeignKeys: {fk|
        private readonly I$fk.ReferencedEntitySingular$Service _$fk.ReferencedEntitySingular$Service;
        }$
        private readonly IDistributedCache _cache;
        private readonly ILogger<$entity.Name$Service> _logger;
        private static readonly DistributedCacheEntryOptions _cacheOptions = new() {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(10)
        \};
        private const string CACHE_VERSION_KEY = "$entity.Name$_CacheVersion";

        public $entity.Name$Service(
            I$entity.Name$Repository repository,
            $entity.ForeignKeys: {fk|
            I$fk.ReferencedEntitySingular$Service $fk.ReferencedEntitySingular$Service,
            }$
            IDistributedCache cache,
            ILogger<$entity.Name$Service> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            $entity.ForeignKeys: {fk|
            _$fk.ReferencedEntitySingular$Service = $fk.ReferencedEntitySingular$Service ?? throw new ArgumentNullException(nameof($fk.ReferencedEntitySingular$Service));
            }$
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("Initializing $entity.Name$Service");
        \}

        /// <summary>
        /// Maps an entity to its DTO representation
        /// </summary>
        private static $entity.Name$Dto MapToDto($entity.Name$ entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return new $entity.Name$Dto
            {
                $entity.Properties: {property|
                $property.Name$ = entity.$property.Name$,
                }$
            \};
        \}

        /// <summary>
        /// Maps creation DTO data to an entity
        /// </summary>
        private static $entity.Name$ MapToEntity(Create$entity.Name$Dto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new $entity.Name$
        {
            $entity.Properties: {property|
            $if(!property.IsAutoGenerated)$
                $property.Name$ = dto.$property.Name$,
            $endif$
            }$
            \};
        \}

        /// <summary>
        /// Maps update DTO data to an entity
        /// </summary>
        private static $entity.Name$ MapToEntity(Update$entity.Name$Dto dto, $entity.KeyType$ id)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new $entity.Name$
        {
            $entity.Properties: {property|
            $if(!property.IsKey)$
                    $property.Name$ = dto.$property.Name$,
                $else$
                    $if(property.IsEntityKey)$
                        $property.Name$ = id,
                    $else$
                        $property.Name$ = dto.$property.Name$,
                    $endif$
            $endif$
            }$
            \};
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
                _logger.LogDebug("Operation executed in " + executionTime.ToString() +"ms");

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

        private async Task InvalidateCollectionCacheAsync()
        {
            try 
            {
                // Increment version to invalidate all cached queries
                var version = await _cache.GetStringAsync(CACHE_VERSION_KEY) ?? "0";
                var newVersion = (int.Parse(version) + 1).ToString();
                await _cache.SetStringAsync(CACHE_VERSION_KEY, newVersion, _cacheOptions);
            \}
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate collection cache");
            \}
        \}

        public async Task<DataPagedResult<$entity.Name$Dto>> GetAllAsync(DataRequestParametersDto? parameters = null) {
            try {
                var version = await _cache.GetStringAsync(CACHE_VERSION_KEY) ?? "0";
                var cacheKey = "$entity.Name$_GetAll_" + version + "_" + Utils.ComputeMd5Hash(JsonConvert.SerializeObject(parameters));
                return await ExecuteCacheOperationAsync("GetAll", cacheKey, async () => {
                    var entitiesResult = await _repository.GetAllAsync(parameters);

                    List<ForeignKeyDataDto> resultForeignKeys = new List<ForeignKeyDataDto>();
                    
                    foreach (ForeignKeyIncludeDto include in parameters.Includes)
                    {
                        ForeignKeyDataDto foreignKeyDataDto = new ForeignKeyDataDto();
                        switch (include.ForeignKey)
                        {
                            default:
                                _logger.LogWarning("Unknown foreign key " + include.ForeignKey + " requested for entity $entity.Name$");
                                break;
                            $entity.ForeignKeys: {fk|
                            case "$fk.Name$":
                            {
                                _logger.LogInformation("Handling foreign key " + include.ForeignKey + " for entity Orders");
                                List<ColumnSearchDto> searchDto = entitiesResult.Items
                                                                        .Select(i => i.$fk.Name$)
                                                                        .Select(i => new ColumnSearchDto() { Column = "$fk.Name$", Value = i.ToString() \})
                                                                        .ToList();
                                _logger.LogInformation("Ids for $fk.Name$:" + string.Join(",", searchDto.Select(s => s.Value)));
                                var foreignKeyData = await _$fk.ReferencedEntitySingular$Service.GetAllAsync(new DataRequestParametersDto()
                                {
                                    Includes = new List<ForeignKeyIncludeDto>(),
                                    ColumnSearches = searchDto,
                                    GlobalSearch = "",
                                    OrderBy = new List<OrderByParameterDto>(),
                                    PageNumber = 0,
                                    PageSize = 0
                                \});
                                
                                foreignKeyDataDto.ForeignKey = "$fk.Name$";
                                foreignKeyDataDto.Values = foreignKeyData.Items.Select(i =>
                                    new ForeignKeyValueDto()
                                        { Id = i.KeyIdsAsString(), Value = Utils.FormatForeignKeyValue(i, include) \});
                                
                                resultForeignKeys.Add(foreignKeyDataDto);
                                break;
                            \}
                            }$
                        \}
                    \}

                    return new DataPagedResult<$entity.Name$Dto>(
                        entitiesResult.Items.Select(e => MapToDto(e)),
                        entitiesResult.Total,
                        parameters,
                        resultForeignKeys
                    );
                \});
            \}
            catch (Exception ex) when (ex is not DynamicContextException) {
                throw new DatabaseOperationException("GetAll", "$entity.Name$", ex);
            \}
        \}

        public async Task<$entity.Name$Dto?> GetByIdAsync($entity.KeyType$ id) {
            if (id == null) {
                throw new EntityValidationException("$entity.Name$", "Id cannot be null");
            \}

            try {
                var cacheKey = "$entity.Name$_GetById_" + id.ToString();
                var result = await ExecuteCacheOperationAsync<$entity.Name$Dto?>("GetById", cacheKey, async () => {
                    var entity = await _repository.GetByIdAsync(id);
                    if (entity == null) {
                        throw new EntityNotFoundException("$entity.Name$", id);
                    \}
                    return MapToDto(entity);
                \});

                return result;
            \}
            catch (Exception ex) when (ex is not DynamicContextException) {
                throw new DatabaseOperationException("GetById", "$entity.Name$", ex);
            \}
        \}

        public async Task<$entity.Name$Dto> CreateAsync(Create$entity.Name$Dto createDto) {
            if (createDto == null) {
                throw new EntityValidationException("$entity.Name$", "Create DTO cannot be null");
            \}

            try {
                var entity = MapToEntity(createDto);
                var result = await _repository.CreateAsync(entity);
                
                // Invalidate collection cache
                await InvalidateCollectionCacheAsync();
                
                _logger.LogInformation("Created new $entity.Name$ with id " + result.$entity.KeyName$.ToString());
                return MapToDto(result);
            \}
            catch (Exception ex) when (ex is not DynamicContextException) {
                throw new DatabaseOperationException("Create", "$entity.Name$", ex);
            \}
        \}

        public async Task<$entity.Name$Dto?> UpdateAsync($entity.KeyType$ id, Update$entity.Name$Dto updateDto) {
            if (id == null) {
                throw new EntityValidationException("$entity.Name$", "Id cannot be null");
            \}
            if (updateDto == null) {
                throw new EntityValidationException("$entity.Name$", "Update DTO cannot be null");
            \}

            try {
                var entity = MapToEntity(updateDto, id);
                var result = await _repository.UpdateAsync(entity);
                
                if (result == null) {
                    throw new EntityNotFoundException("$entity.Name$", id);
                \}

                // Invalidate individual cache
                var cacheKey = "$entity.Name$_GetById_" + id.ToString();
                await _cache.RemoveAsync(cacheKey);

                // Invalidate collection cache
                await InvalidateCollectionCacheAsync();

                _logger.LogInformation("Updated $entity.Name$ with id " + id.ToString());
                return MapToDto(result);
            \}
            catch (Exception ex) when (ex is not DynamicContextException) {
                throw new DatabaseOperationException("Update", "$entity.Name$", ex);
            \}
        \}

        public async Task<bool> DeleteAsync($entity.KeyType$ id) {
            if (id == null) {
                throw new EntityValidationException("$entity.Name$", "Id cannot be null");
            \}

            try {
                var deleted = await _repository.DeleteAsync(id);
                
                if (!deleted) {
                    throw new EntityNotFoundException("$entity.Name$", id);
                \}

                // Invalidate individual cache
                var cacheKey = "$entity.Name$_GetById_" + id.ToString();
                await _cache.RemoveAsync(cacheKey);

                // Invalidate collection cache
                await InvalidateCollectionCacheAsync();

                _logger.LogInformation("Deleted $entity.Name$ with id " + id.ToString());
                return true;
            \}
            catch (Exception ex) when (ex is not DynamicContextException) {
                throw new DatabaseOperationException("Delete", "$entity.Name$", ex);
            \}
        \}

        public async Task<List<string>> ColumnUniqueValuesAsync(string column) {
            if (string.IsNullOrEmpty(column)) {
                throw new EntityValidationException("$entity.Name$", "Column name cannot be null or empty");
            \}

            try {
                var cacheKey = "$entity.Name$_UniqueValues_" + column;
                return await ExecuteCacheOperationAsync("GetUniqueValues", cacheKey, async () => {
                    var values = await _repository.GetUniqueValuesForColumnAsync(column);
                    return values ?? new List<string>();
                \});
            \}
            catch (Exception ex) when (ex is not DynamicContextException) {
                throw new DatabaseOperationException("GetUniqueValues", "$entity.Name$", ex);
            \}
        \}
    \}
    #endregion
    }$
\} 