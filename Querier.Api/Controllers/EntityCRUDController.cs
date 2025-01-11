using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.DTOs.Requests.Entity;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Common.ValueObjects;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for handling CRUD operations on entities
    /// </summary>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class EntityCrudController(IEntityCrudService entityCrudService, ILogger<EntityCrudController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Gets all available database contexts
        /// </summary>
        [HttpGet("GetContexts")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetContexts()
        {
            try
            {
                logger.LogInformation("Getting all database contexts");
                var contexts = await entityCrudService.GetContextsAsync();
                logger.LogInformation("Retrieved {Count} contexts", contexts.Count);
                return Ok(contexts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving database contexts");
                return StatusCode(500, "An error occurred while retrieving database contexts");
            }
        }

        /// <summary>
        /// Gets all entities for a specific context
        /// </summary>
        [HttpGet("GetEntities")]
        [ProducesResponseType(typeof(EntityDefinition[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetEntities([FromQuery] string contextTypeName)
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeName))
                {
                    logger.LogWarning("GetEntities called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                logger.LogInformation("Getting entities for context: {Context}", contextTypeName);
                var entities = entityCrudService.GetEntities(contextTypeName);
                logger.LogInformation("Retrieved {Count} entities from context {Context}", 
                    entities.Count, contextTypeName);
                return Ok(entities);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting entities for context {Context}", contextTypeName);
                return StatusCode(500, "An error occurred while retrieving entities");
            }
        }

        /// <summary>
        /// Gets a specific entity from a context
        /// </summary>
        [HttpGet("GetEntity")]
        [ProducesResponseType(typeof(EntityDefinition), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetEntity([FromQuery] string contextTypeName, [FromQuery] string entityName)
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeName))
                {
                    logger.LogWarning("GetEntity called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                if (string.IsNullOrEmpty(entityName))
                {
                    logger.LogWarning("GetEntity called with null or empty entity name");
                    return BadRequest("Entity name is required");
                }

                logger.LogInformation("Getting entity {Entity} from context {Context}", 
                    entityName, contextTypeName);
                var entity = entityCrudService.GetEntity(contextTypeName, entityName);

                if (entity == null)
                {
                    logger.LogWarning("Entity {Entity} not found in context {Context}", 
                        entityName, contextTypeName);
                    return NotFound($"Entity {entityName} not found in context {contextTypeName}");
                }

                logger.LogInformation("Successfully retrieved entity {Entity}", entityName);
                return Ok(entity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting entity {Entity} from context {Context}", 
                    entityName, contextTypeName);
                return StatusCode(500, "An error occurred while retrieving the entity");
            }
        }

        /// <summary>
        /// Gets all records for a specific entity with pagination
        /// </summary>
        [HttpGet("GetAll")]
        [ProducesResponseType(typeof(PagedResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAll(
            [FromQuery] string contextTypeName, 
            [FromQuery] string entityTypeName,
            [FromQuery] int pageNumber = 0,
            [FromQuery] int pageSize = 0,
            [FromQuery] string orderBy = "")
        {
            try
            {
                if (string.IsNullOrEmpty(contextTypeName))
                {
                    logger.LogWarning("GetAll called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                if (string.IsNullOrEmpty(entityTypeName))
                {
                    logger.LogWarning("GetAll called with null or empty entity type name");
                    return BadRequest("Entity type name is required");
                }

                logger.LogInformation("Getting all records for entity {Entity} from context {Context} (page {Page}, size {Size})", 
                    entityTypeName, contextTypeName, pageNumber, pageSize);

                var paginationParams = new PaginationParameters 
                { 
                    PageNumber = pageNumber,
                    PageSize = pageSize 
                };
                
                var result = entityCrudService.GetAll(contextTypeName, entityTypeName, paginationParams, orderBy);
                logger.LogInformation("Retrieved {Count} records for entity {Entity}", 
                    result.Items.Count(), entityTypeName);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation getting records for entity {Entity}", entityTypeName);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting records for entity {Entity} from context {Context}", 
                    entityTypeName, contextTypeName);
                return StatusCode(500, "An error occurred while retrieving records");
            }
        }

        /// <summary>
        /// Executes a custom SQL query on an entity
        /// </summary>
        [HttpPost("ReadFromSql")]
        [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ReadFromSql([FromBody] EntityCRUDReadSqlQueryDto request)
        {
            try
            {
                if (request == null)
                {
                    logger.LogWarning("ReadFromSql called with null request");
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrEmpty(request.ContextTypeName))
                {
                    logger.LogWarning("ReadFromSql called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                if (string.IsNullOrEmpty(request.SqlQuery))
                {
                    logger.LogWarning("ReadFromSql called with null or empty SQL query");
                    return BadRequest("SQL query is required");
                }

                logger.LogInformation("Executing SQL query in context {Context}", request.ContextTypeName);
                var datas = entityCrudService.ReadFromSql(request.ContextTypeName, request.SqlQuery, request.Filters);
                logger.LogInformation("Successfully executed SQL query in context {Context}", request.ContextTypeName);
                return Ok(datas);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing SQL query in context {Context}", request?.ContextTypeName);
                return StatusCode(500, "An error occurred while executing the SQL query");
            }
        }

        /// <summary>
        /// Creates a new entity record
        /// </summary>
        [HttpPost("Create")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Create([FromBody] EntityCRUDCreateOrUpdateDto model)
        {
            try
            {
                if (model == null)
                {
                    logger.LogWarning("Create called with null model");
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrEmpty(model.ContextTypeName))
                {
                    logger.LogWarning("Create called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                if (string.IsNullOrEmpty(model.EntityType))
                {
                    logger.LogWarning("Create called with null or empty entity type");
                    return BadRequest("Entity type is required");
                }

                if (model.Data == null)
                {
                    logger.LogWarning("Create called with null data");
                    return BadRequest("Entity data is required");
                }

                logger.LogInformation("Creating new entity of type {EntityType} in context {Context}", 
                    model.EntityType, model.ContextTypeName);
                var result = entityCrudService.Create(model.ContextTypeName, model.EntityType, model.Data);
                logger.LogInformation("Successfully created new entity of type {EntityType}", model.EntityType);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation creating entity of type {EntityType}", model?.EntityType);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating entity of type {EntityType} in context {Context}", 
                    model?.EntityType, model?.ContextTypeName);
                return StatusCode(500, "An error occurred while creating the entity");
            }
        }

        /// <summary>
        /// Updates an existing entity record
        /// </summary>
        [HttpPost("Update")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Update([FromBody] EntityCRUDCreateOrUpdateDto model)
        {
            try
            {
                if (model == null)
                {
                    logger.LogWarning("Update called with null model");
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrEmpty(model.ContextTypeName))
                {
                    logger.LogWarning("Update called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                if (string.IsNullOrEmpty(model.EntityType))
                {
                    logger.LogWarning("Update called with null or empty entity type");
                    return BadRequest("Entity type is required");
                }

                if (model.Data == null)
                {
                    logger.LogWarning("Update called with null data");
                    return BadRequest("Entity data is required");
                }

                logger.LogInformation("Updating entity of type {EntityType} in context {Context}", 
                    model.EntityType, model.ContextTypeName);
                var result = entityCrudService.Update(model.ContextTypeName, model.EntityType, model.Data);
                logger.LogInformation("Successfully updated entity of type {EntityType}", model.EntityType);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation updating entity of type {EntityType}", model?.EntityType);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating entity of type {EntityType} in context {Context}", 
                    model?.EntityType, model?.ContextTypeName);
                return StatusCode(500, "An error occurred while updating the entity");
            }
        }

        /// <summary>
        /// Creates or updates an entity record
        /// </summary>
        [HttpPost("CreateOrUpdate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CreateOrUpdate([FromBody] EntityCRUDCreateOrUpdateDto model)
        {
            try
            {
                if (model == null)
                {
                    logger.LogWarning("CreateOrUpdate called with null model");
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrEmpty(model.ContextTypeName))
                {
                    logger.LogWarning("CreateOrUpdate called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                if (string.IsNullOrEmpty(model.EntityType))
                {
                    logger.LogWarning("CreateOrUpdate called with null or empty entity type");
                    return BadRequest("Entity type is required");
                }

                if (model.Data == null)
                {
                    logger.LogWarning("CreateOrUpdate called with null data");
                    return BadRequest("Entity data is required");
                }

                logger.LogInformation("Creating or updating entity of type {EntityType} in context {Context}", 
                    model.EntityType, model.ContextTypeName);
                var result = entityCrudService.CreateOrUpdate(model.ContextTypeName, model.EntityType, model.Data);
                logger.LogInformation("Successfully created or updated entity of type {EntityType}", model.EntityType);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation creating or updating entity of type {EntityType}", 
                    model?.EntityType);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating or updating entity of type {EntityType} in context {Context}", 
                    model?.EntityType, model?.ContextTypeName);
                return StatusCode(500, "An error occurred while creating or updating the entity");
            }
        }

        /// <summary>
        /// Deletes an entity record
        /// </summary>
        [HttpDelete("Delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Delete([FromBody] EntityCRUDDeleteDto model)
        {
            try
            {
                if (model == null)
                {
                    logger.LogWarning("Delete called with null model");
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrEmpty(model.ContextTypeName))
                {
                    logger.LogWarning("Delete called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                if (string.IsNullOrEmpty(model.EntityType))
                {
                    logger.LogWarning("Delete called with null or empty entity type");
                    return BadRequest("Entity type is required");
                }

                if (model.Key == null)
                {
                    logger.LogWarning("Delete called with null key");
                    return BadRequest("Entity key is required");
                }

                logger.LogInformation("Deleting entity of type {EntityType} in context {Context}", 
                    model.EntityType, model.ContextTypeName);
                entityCrudService.Delete(model.ContextTypeName, model.EntityType, model.Key);
                logger.LogInformation("Successfully deleted entity of type {EntityType}", model.EntityType);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation deleting entity of type {EntityType}", model?.EntityType);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting entity of type {EntityType} in context {Context}", 
                    model?.EntityType, model?.ContextTypeName);
                return StatusCode(500, "An error occurred while deleting the entity");
            }
        }

        /// <summary>
        /// Gets the entity definition for a SQL query
        /// </summary>
        [HttpPost("GetSQLQueryEntityDefinition")]
        [ProducesResponseType(typeof(SQLQueryResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetSqlQueryEntityDefinition([FromBody] EntityCRUDExecuteSQLQueryDto request)
        {
            try
            {
                if (request == null)
                {
                    logger.LogWarning("GetSQLQueryEntityDefinition called with null request");
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrEmpty(request.ContextTypeName))
                {
                    logger.LogWarning("GetSQLQueryEntityDefinition called with null or empty context type name");
                    return BadRequest("Context type name is required");
                }

                if (string.IsNullOrEmpty(request.SqlQuery))
                {
                    logger.LogWarning("GetSQLQueryEntityDefinition called with null or empty SQL query");
                    return BadRequest("SQL query is required");
                }

                logger.LogInformation("Getting entity definition for SQL query in context {Context}", 
                    request.ContextTypeName);
                var result = entityCrudService.GetSqlQueryEntityDefinition(request);

                if (!result.QuerySuccessful)
                {
                    logger.LogWarning("SQL query execution failed in context {Context}: {Error}", 
                        request.ContextTypeName, result.ErrorMessage);
                    return BadRequest(result.ErrorMessage);
                }

                logger.LogInformation("Successfully retrieved entity definition for SQL query in context {Context}", 
                    request.ContextTypeName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting entity definition for SQL query in context {Context}", 
                    request?.ContextTypeName);
                return StatusCode(500, "An error occurred while getting the entity definition");
            }
        }
    }
}
