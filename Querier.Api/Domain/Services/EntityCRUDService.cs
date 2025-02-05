﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Common.Extensions;
using Querier.Api.Common.Utilities;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Tools;
using Querier.Api.Infrastructure.Services;
using DataTable = System.Data.DataTable;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Querier.Api.Domain.Services
{
    public class EntityCrudService(IDbConnectionRepository dbConnectionRepository, ILogger<EntityCrudService> logger)
        : IDatasourcesService
    {
        public async Task<List<string>> GetContextsAsync()
        {
            try
            {
                logger.LogInformation("Retrieving all database contexts");
                var contexts = new List<string>();
                var dbConnections = await dbConnectionRepository.GetAllDbConnectionsAsync();
                
                logger.LogDebug("Processing {Count} database connections", dbConnections.Count);
                foreach (var dbConnection in dbConnections)
                {
                    try
                    {
                        logger.LogTrace("Loading assembly for connection {Name}", dbConnection.Name);
                        
                        var assembly = Assembly.Load(await dbConnectionRepository.GetDLLStreamAsync(dbConnection.Id));
                        var types = assembly.GetTypes()
                            .Where(t => t.IsAssignableTo(typeof(DbContext)));

                        IEnumerable<Type> enumerableTypes = types as Type[] ?? types.ToArray();
                        contexts.AddRange(enumerableTypes.Select(t => t.FullName));
                        logger.LogDebug("Found {Count} contexts in connection {Name}", enumerableTypes.Count(), dbConnection.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error loading assembly for context {Name}", dbConnection.Name);
                    }
                }

                logger.LogInformation("Retrieved {Count} total contexts", contexts.Count);
                return contexts;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving database contexts");
                throw;
            }
        }

        public List<DataStructureDefinitionDto> GetEntities(string contextTypeFullname)
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeFullname))
                {
                    throw new ArgumentException("Context type name is required", nameof(contextTypeFullname));
                }

                logger.LogInformation("Getting entities for context: {Context}", contextTypeFullname);
                List<DataStructureDefinitionDto> result = new List<DataStructureDefinitionDto>();
                
                DbContext targetContext = Utils.GetDbContextFromTypeName(contextTypeFullname);
                if (targetContext == null)
                {
                    logger.LogWarning("Context not found: {Context}", contextTypeFullname);
                    return result;
                }

                List<PropertyInfo> contextDbSetProperties = targetContext.GetType().GetProperties()
                    .Where(p => p.PropertyType.Name.Contains("DbSet")).ToList();
                
                logger.LogDebug("Found {Count} DbSet properties in context {Context}", 
                    contextDbSetProperties.Count, contextTypeFullname);

                foreach (PropertyInfo pi in contextDbSetProperties)
                {
                    try
                    {
                        var entityType = pi.PropertyType.GetGenericArguments().First();
                        result.Add(entityType.ToEntityDefinition(targetContext));
                        logger.LogTrace("Added entity definition for {Entity}", entityType.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing DbSet property {Property}", pi.Name);
                    }
                }

                logger.LogInformation("Retrieved {Count} entities from context {Context}", 
                    result.Count, contextTypeFullname);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting entities for context {Context}", contextTypeFullname);
                throw;
            }
        }

        public DataStructureDefinitionDto GetEntity(string contextTypeFullname, string entityFullname)
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeFullname))
                {
                    throw new ArgumentException("Context type name is required", nameof(contextTypeFullname));
                }

                if (string.IsNullOrEmpty(entityFullname))
                {
                    throw new ArgumentException("Entity name is required", nameof(entityFullname));
                }

                logger.LogInformation("Getting entity {Entity} from context {Context}", 
                    entityFullname, contextTypeFullname);

                var entities = GetEntities(contextTypeFullname);
                var entity = entities.FirstOrDefault(e => e.Name == entityFullname);

                if (entity == null)
                {
                    logger.LogWarning("Entity {Entity} not found in context {Context}", 
                        entityFullname, contextTypeFullname);
                }
                else
                {
                    logger.LogDebug("Successfully retrieved entity {Entity}", entityFullname);
                }

                return entity;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting entity {Entity} from context {Context}", 
                    entityFullname, contextTypeFullname);
                throw;
            }
        }

        public int Create(string contextTypeFullname, string entityTypeFullname, dynamic entity)
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeFullname))
                {
                    throw new ArgumentException("Context type name is required", nameof(contextTypeFullname));
                }

                if (string.IsNullOrEmpty(entityTypeFullname))
                {
                    throw new ArgumentException("Entity type name is required", nameof(entityTypeFullname));
                }

                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(entity));
                }

                logger.LogInformation("Creating new entity of type {EntityType} in context {Context}", 
                    entityTypeFullname, contextTypeFullname);

            Type entityType = Utils.GetType(entityTypeFullname);
            if (entityType == null)
                {
                    var message = $"Entity \"{entityTypeFullname}\" is not handled in the {contextTypeFullname} context.";
                    logger.LogError(message);
                    throw new InvalidOperationException(message);
                }

            DbContext targetContext = Utils.GetDbContextFromTypeName(contextTypeFullname);
                if (targetContext == null)
                {
                    throw new InvalidOperationException($"Context {contextTypeFullname} not found");
                }

            object newEntity = Activator.CreateInstance(entityType);
            dynamic modelEntity = JsonSerializer.Deserialize(entity.ToString(), entityType);

                logger.LogDebug("Mapping properties for new entity");
                if (newEntity != null)
                {
                    foreach (PropertyInfo pi in newEntity.GetType().GetProperties()
                                 .Where(p => p.GetCustomAttribute(typeof(NotMappedAttribute)) == null &&
                         p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null))
                    {
                        try
            {
                string propertyName = pi.Name;
                object value = modelEntity.GetType().GetProperty(propertyName).GetValue(modelEntity, null);
                pi.SetValue(newEntity, value);
                            logger.LogTrace("Mapped property {Property} with value {Value}", propertyName, value);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error mapping property {Property}", pi.Name);
                        }
                    }

                    logger.LogDebug("Adding entity to context");
                    targetContext.Add(newEntity);

                    logger.LogDebug("Saving changes to database");
                    int result = targetContext.SaveChanges();

                    logger.LogInformation("Successfully created new entity of type {EntityType}", entityTypeFullname);
                    return result;
                }
            }
            catch (DbUpdateException ex)
            {
                logger.LogError(ex, "Database error creating entity of type {EntityType}", entityTypeFullname);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating entity of type {EntityType}", entityTypeFullname);
                throw;
            }

            return 0;
        }

        public DataPagedResult<object> GetAll(string contextTypeFullname, string entityTypeFullname, 
            DataRequestParametersDto dataRequestParameters)
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeFullname))
                {
                    throw new ArgumentException("Context type name is required", nameof(contextTypeFullname));
                }

                if (string.IsNullOrEmpty(entityTypeFullname))
                {
                    throw new ArgumentException("Entity type name is required", nameof(entityTypeFullname));
                }

                if (dataRequestParameters == null)
                {
                    throw new ArgumentNullException(nameof(dataRequestParameters));
                }

                logger.LogInformation("Retrieving all entities of type {EntityType} from context {Context}", 
                    entityTypeFullname, contextTypeFullname);

                Type reqType = Utils.GetType(entityTypeFullname);
                if (reqType == null)
                {
                    var message = $"Entity \"{entityTypeFullname}\" is not handled in the \"{contextTypeFullname}\" context.";
                    logger.LogError(message);
                    throw new InvalidOperationException(message);
                }

                DbContext targetContext = Utils.GetDbContextFromTypeName(contextTypeFullname);
                if (targetContext == null)
                {
                    throw new InvalidOperationException($"Context {contextTypeFullname} not found");
                }

                targetContext.ChangeTracker.LazyLoadingEnabled = false;

                var dbSet = targetContext.GetType()
                    .GetProperties()
                    .Where(p => p.PropertyType.Name.Contains("DbSet"))
                    .FirstOrDefault(p => p.PropertyType.GetGenericArguments().Any(a => a == reqType))
                    ?.GetValue(targetContext);

                if (dbSet == null)
                {
                    var message = $"Entity \"{entityTypeFullname}\" is not handled by any DbSet in the \"{contextTypeFullname}\" context.";
                    logger.LogError(message);
                    throw new InvalidOperationException(message);
                }

                // Get the DbSet as IQueryable
                var query = ((IQueryable<object>)dbSet);

                // Apply search filters
                if (!string.IsNullOrEmpty(dataRequestParameters.GlobalSearch))
                {
                    logger.LogDebug("Applying global search filter: {Search}", dataRequestParameters.GlobalSearch);
                    // Load data in memory for complex search operations
                    var searchTerm = dataRequestParameters.GlobalSearch.ToLower();
                    var searchResults = query.AsEnumerable()
                        .Where(e => e.GetType()
                        .GetProperties()
                        .Where(p => p.PropertyType == typeof(string))
                            .Any(p => ((string)p.GetValue(e, null) ?? string.Empty)
                                .ToLower()
                                .Contains(searchTerm)))
                        .AsQueryable();
                    query = searchResults;
                }

                // Apply column-specific searches
                if (dataRequestParameters.ColumnSearches?.Any() == true)
                {
                    logger.LogDebug("Applying column-specific searches");
                    var searchResults = query.AsEnumerable();
                    foreach (var columnSearch in dataRequestParameters.ColumnSearches)
                    {
                        var searchTerm = columnSearch.Value.ToLower();
                        var columnName = columnSearch.Column;
                        searchResults = searchResults.Where(e => 
                            ((string)e.GetType().GetProperty(columnName)?.GetValue(e, null) ?? string.Empty)
                            .ToLower()
                            .Contains(searchTerm));
                    }
                    query = searchResults.AsQueryable();
                }

                // Apply sorting
                var sortedData = query.AsEnumerable();
                if (dataRequestParameters.OrderBy?.Any() == true)
                {
                    logger.LogDebug("Applying sorting");
                    var isFirst = true;
                    IOrderedEnumerable<object> orderedData = null;

                    foreach (var orderBy in dataRequestParameters.OrderBy)
                    {
                        if (isFirst)
                        {
                            orderedData = orderBy.IsDescending
                            ? sortedData.OrderByDescending(e => GetPropertyValue(e, orderBy.Column))
                            : sortedData.OrderBy(e => GetPropertyValue(e, orderBy.Column));
                            isFirst = false;
                        }
                        else
                        {
                            orderedData = orderBy.IsDescending
                                ? orderedData.ThenByDescending(e => GetPropertyValue(e, orderBy.Column))
                                : orderedData.ThenBy(e => GetPropertyValue(e, orderBy.Column));
                        }
                    }
                    sortedData = orderedData ?? sortedData;
                }

                // Get total count before pagination
                var totalCount = sortedData.Count();
                logger.LogDebug("Total count before pagination: {Count}", totalCount);

                // Apply pagination
                var data = sortedData
                    .Skip(dataRequestParameters.PageNumber != 0 ? (dataRequestParameters.PageNumber - 1) * dataRequestParameters.PageSize : 0)
                    .Take(dataRequestParameters.PageNumber != 0 ? dataRequestParameters.PageSize : totalCount)
                    .Select(e => e.GetType()
                        .GetProperties()
                        .Where(p => p.PropertyType.Namespace == "System" || p.PropertyType.IsValueType)
                        .ToDictionary(
                            p => p.Name,
                            p => p.GetValue(e)
                        ));

                var result = new DataPagedResult<object>(data, totalCount, dataRequestParameters);
                logger.LogInformation("Retrieved {Count} entities of type {EntityType} (page {Page}, size {Size})", 
                    result.Items.Count(), entityTypeFullname, dataRequestParameters.PageNumber, dataRequestParameters.PageSize);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving entities of type {EntityType}", entityTypeFullname);
                throw;
            }
        }

        public IEnumerable<object> Read(string contextTypeFullname, string entityTypeFullname, List<DataFilterDto> filters)
        {
            return Read(contextTypeFullname, entityTypeFullname, filters, out _);
        }

        public IEnumerable<object> Read(string contextTypeFullname, string entityTypeFullname, List<DataFilterDto> filters, out Type entityType)
        {
            Type reqType = Utils.GetType(entityTypeFullname);
            if (reqType == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled in the \"{contextTypeFullname}\" context.");

            DbContext targetContext = Utils.GetDbContextFromTypeName(contextTypeFullname);

            PropertyInfo contextProperty = targetContext.GetType().GetProperties().Where(p => p.PropertyType.Name.Contains("DbSet")).FirstOrDefault(p => p.PropertyType.GetGenericArguments().Any(a => a == reqType));
            if (contextProperty == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled by any DbSet in the \"{contextTypeFullname}\" context.");

            entityType = contextProperty.PropertyType.GetGenericArguments()[0];
            var dbsetResult = contextProperty.GetValue(targetContext) as IEnumerable<object>;
            var dt = (DataTable)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dbsetResult), typeof(DataTable));
            dt = dt.Filter(filters);
            return JsonConvert.DeserializeObject<List<ExpandoObject>>(JsonConvert.SerializeObject(dt));
        }

        public DataTable GetDatatableFromSql(string contextTypeFullname, string sqlQuery, List<DataFilterDto> filters)
        {
            DbContext apiDbContext = Utils.GetDbContextFromTypeName(contextTypeFullname);
            DataTable dt = apiDbContext.Database.RawSqlQuery(sqlQuery);
            return dt.Filter(filters);
        }

        public IEnumerable<object> ReadFromSql(string contextTypeFullname, string sqlQuery, List<DataFilterDto> filters)
        {
            DataTable dt = GetDatatableFromSql(contextTypeFullname, sqlQuery, filters);
            var res = JsonConvert.DeserializeObject<List<ExpandoObject>>(JsonConvert.SerializeObject(dt));
            return res;
        }

        public int Update(string contextTypeFullname, string entityFullname, object entity)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            DbContext targetContext = Utils.GetDbContextFromTypeName(contextTypeFullname);

            object modelEntity = JsonSerializer.Deserialize(entity.ToString() ?? throw new InvalidOperationException(), entityType);
            if (modelEntity != null)
            {
                object keyValue = modelEntity.GetType().GetProperty(keyProperty.Name)?.GetValue(modelEntity, null);

            object existingEntity = targetContext.Find(entityType, keyValue);

            foreach (PropertyInfo pi in modelEntity.GetType().GetProperties().Where(p => p.GetCustomAttribute(typeof(NotMappedAttribute)) == null &&
                         p.GetCustomAttribute(typeof(JsonIgnoreAttribute)) == null))
            {
                if (pi.Name == keyProperty.Name)
                    continue;

                string propertyName = pi.Name;
                    object value = modelEntity.GetType().GetProperty(propertyName)?.GetValue(modelEntity, null);
                pi.SetValue(existingEntity, value);
                }

                return targetContext.SaveChanges();
            }

            return 0;
        }

        public int CreateOrUpdate(string contextTypeFullname, string entityFullname, object entity)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            DbContext targetContext = Utils.GetDbContextFromTypeName(contextTypeFullname);

            object modelEntity = JsonSerializer.Deserialize(entity.ToString() ?? string.Empty, entityType);
            if (modelEntity != null)
            {
                object keyValue = modelEntity.GetType().GetProperty(keyProperty.Name)?.GetValue(modelEntity, null);

            object existingEntity = targetContext.Find(entityType, keyValue);

            if (existingEntity is null)
                return Create(contextTypeFullname, entityFullname, entity);
            }

            return Update(contextTypeFullname, entityFullname, entity);
        }

        public void Delete(string contextTypeFullname, string entityFullname, object entityIdentifier)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            DbContext targetContext = Utils.GetDbContextFromTypeName(contextTypeFullname);

            object entityKey = Convert.ChangeType(entityIdentifier, keyProperty.PropertyType);
            object existingEntity = targetContext.Find(entityType, entityKey);

            if (existingEntity != null) targetContext.Remove(existingEntity);
            targetContext.SaveChanges();
        }

        public SqlQueryResultDto GetSqlQueryEntityDefinition(EntityCRUDExecuteSQLQueryDto request)
        {
            SqlQueryResultDto result = new SqlQueryResultDto
            {
                QuerySuccessful = true,
                Datas = []
            };
            DbContext apiDbContext = Utils.GetDbContextFromTypeName(request.ContextTypeName);

            try
            {
                DataTable dt = apiDbContext.Database.RawSqlQuery(request.SqlQuery);
                result.Structure = new DataStructureDefinitionDto
                {
                    Name = "SQL Query Result",
                    Description = "Structure of SQL query result",
                    Type = "object",
                    SourceType = DataSourceType.Entity,
                    JsonSchema = new JsonSchemaGeneratorService(logger).GenerateFromDataTable(dt, "SQL Query Result")
                };

                result.Datas = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(dt));
            }
            catch (Exception e)
            {
                result.QuerySuccessful = false;
                result.ErrorMessage = e.Message;
            }

            return result;
        }

        private static object GetPropertyValue(object obj, string propertyName)
        {
            return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null) ?? DBNull.Value;
        }
    }
}
