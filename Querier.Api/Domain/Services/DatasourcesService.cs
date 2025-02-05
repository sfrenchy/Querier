using System;
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
using Querier.Api.Domain.Entities.DBConnection;
using Querier.Api.Tools;
using Querier.Api.Infrastructure.Services;
using DataTable = System.Data.DataTable;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Querier.Api.Domain.Services
{
    public class DatasourcesService(IDbConnectionRepository dbConnectionRepository, IDbConnectionService connectionService, ILogger<DatasourcesService> logger)
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

        public async Task<List<DataStructureDefinitionDto>> GetEntities(string contextTypeFullname)
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeFullname))
                {
                    throw new ArgumentException("Context type name is required", nameof(contextTypeFullname));
                }

                logger.LogInformation("Getting entities for context: {Context}", contextTypeFullname);
                List<DataStructureDefinitionDto> result = new List<DataStructureDefinitionDto>();
                
                var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(contextTypeFullname);
                using DbContext targetContext = await contextFactory.CreateDbContextAsync();
                
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

        public async Task<DataStructureDefinitionDto> GetEntity(string contextTypeFullname, string entityFullname)
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

                var entities = await GetEntities(contextTypeFullname);
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

        public async Task<int> Create(string contextTypeFullname, string entityTypeFullname, dynamic entity)
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

                var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(contextTypeFullname);
                using DbContext targetContext = await contextFactory.CreateDbContextAsync();
                
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

        public async Task<DataPagedResult<object>> GetAll(string contextTypeFullname, string entityTypeFullname, 
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

                var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(contextTypeFullname);
                using DbContext targetContext = await contextFactory.CreateDbContextAsync();
                
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
                var result = query.ApplyDataRequestParametersDto(dataRequestParameters);
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

        public async Task<IEnumerable<object>> Read(string contextTypeFullname, string entityTypeFullname, List<DataFilterDto> filters)
        {
            Type reqType = Utils.GetType(entityTypeFullname);
            if (reqType == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled in the \"{contextTypeFullname}\" context.");

            var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(contextTypeFullname);
            using DbContext targetContext = await contextFactory.CreateDbContextAsync();

            PropertyInfo contextProperty = targetContext.GetType().GetProperties().Where(p => p.PropertyType.Name.Contains("DbSet")).FirstOrDefault(p => p.PropertyType.GetGenericArguments().Any(a => a == reqType));
            if (contextProperty == null)
                throw new Exception($"Entity \"{entityTypeFullname}\" is not handled by any DbSet in the \"{contextTypeFullname}\" context.");

            // entityType = contextProperty.PropertyType.GetGenericArguments()[0];
            var dbsetResult = contextProperty.GetValue(targetContext) as IEnumerable<object>;
            var dt = (DataTable)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(dbsetResult), typeof(DataTable));
            dt = dt.Filter(filters);
            return JsonConvert.DeserializeObject<List<ExpandoObject>>(JsonConvert.SerializeObject(dt));
        }

        public async Task<DataTable> GetDatatableFromSql(string contextTypeFullname, string sqlQuery, List<DataFilterDto> filters)
        {
            var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(contextTypeFullname);
            using DbContext targetContext = await contextFactory.CreateDbContextAsync();
            DataTable dt = targetContext.Database.RawSqlQuery(sqlQuery);
            return dt.Filter(filters);
        }

        public async Task<IEnumerable<object>> ReadFromSql(string contextTypeFullname, string sqlQuery, List<DataFilterDto> filters)
        {
            DataTable dt = await GetDatatableFromSql(contextTypeFullname, sqlQuery, filters);
            var res = JsonConvert.DeserializeObject<List<ExpandoObject>>(JsonConvert.SerializeObject(dt));
            return res;
        }

        public async Task<int> Update(string contextTypeFullname, string entityFullname, object entity)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(contextTypeFullname);
            using DbContext targetContext = await contextFactory.CreateDbContextAsync();

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

        public async Task<int> CreateOrUpdate(string contextTypeFullname, string entityFullname, object entity)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(contextTypeFullname);
            using DbContext targetContext = await contextFactory.CreateDbContextAsync();

            object modelEntity = JsonSerializer.Deserialize(entity.ToString() ?? string.Empty, entityType);
            if (modelEntity != null)
            {
                object keyValue = modelEntity.GetType().GetProperty(keyProperty.Name)?.GetValue(modelEntity, null);

            object existingEntity = targetContext.Find(entityType, keyValue);

            if (existingEntity is null)
                return await Create(contextTypeFullname, entityFullname, entity);
            }

            return await Update(contextTypeFullname, entityFullname, entity);
        }

        public async Task Delete(string contextTypeFullname, string entityFullname, object entityIdentifier)
        {
            Type entityType = Utils.GetType(entityFullname);
            if (entityType == null)
                throw new Exception($"Entity \"{entityFullname}\" is not handled in the {contextTypeFullname} context.");

            PropertyInfo keyProperty = entityType.GetProperties().FirstOrDefault(p => p.GetCustomAttribute(typeof(KeyAttribute)) != null);
            if (keyProperty == null)
                throw new Exception($"Entity \"{entityFullname}\" has no defined Key attribute defined in the datamart context.");

            var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(contextTypeFullname);
            using DbContext targetContext = await contextFactory.CreateDbContextAsync();

            object entityKey = Convert.ChangeType(entityIdentifier, keyProperty.PropertyType);
            object existingEntity = targetContext.Find(entityType, entityKey);

            if (existingEntity != null) targetContext.Remove(existingEntity);
            targetContext.SaveChanges();
        }

        public async Task<SqlQueryResultDto> GetSqlQueryEntityDefinition(EntityCRUDExecuteSQLQueryDto request)
        {
            SqlQueryResultDto result = new SqlQueryResultDto
            {
                QuerySuccessful = true,
                Datas = []
            };
            
            var contextFactory = await connectionService.GetDbContextFactoryByContextTypeFullNameAsync(request.ContextTypeName);
            using DbContext targetContext = await contextFactory.CreateDbContextAsync();
            
            try
            {
                DataTable dt = targetContext.Database.RawSqlQuery(request.SqlQuery);
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

        
    }
}
