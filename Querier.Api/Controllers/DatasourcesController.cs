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

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing dynamic entity CRUD operations across different database contexts
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Discovering available database contexts and their entities
    /// - Performing CRUD operations on any entity in any context
    /// - Executing custom SQL queries
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/v1/datasources")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class DataSourcesController(IDatasourcesService entityCrudService, ILogger<DataSourcesController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves all available database contexts
        /// </summary>
        /// <returns>Array of context names</returns>
        /// <response code="200">List of available database contexts</response>
        /// <response code="500">If an error occurs while retrieving contexts</response>
        [HttpGet("contexts")]
        [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetContexts()
        {
            try
            {
                logger.LogInformation("Retrieving all database contexts");
                var contexts = await entityCrudService.GetContextsAsync();
                logger.LogInformation("Successfully retrieved {Count} database contexts", contexts.Count);
                return Ok(contexts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve database contexts");
                return StatusCode(500, "An error occurred while retrieving database contexts");
            }
        }

        /// <summary>
        /// Retrieves all entities for a specific database context
        /// </summary>
        /// <param name="contextName">Name of the database context</param>
        /// <returns>Array of entity definitions</returns>
        /// <response code="200">List of entities in the specified context</response>
        /// <response code="400">If the context name is invalid or missing</response>
        /// <response code="500">If an error occurs while retrieving entities</response>
        [HttpGet("contexts/{contextName}/entities")]
        [ProducesResponseType(typeof(DataStructureDefinitionDto[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetEntities(string contextName)
        {
            try
            {
                if (string.IsNullOrEmpty(contextName))
                {
                    logger.LogWarning("Attempted to get entities with null or empty context name");
                    return BadRequest("Context name is required");
                }

                logger.LogInformation("Retrieving entities for context: {ContextName}", contextName);
                var entities = await entityCrudService.GetEntities(contextName);
                logger.LogInformation("Successfully retrieved {Count} entities from context {ContextName}", 
                    entities.Count, contextName);
                return Ok(entities);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve entities for context {ContextName}", contextName);
                return StatusCode(500, "An error occurred while retrieving entities");
            }
        }

        /// <summary>
        /// Retrieves a specific entity definition from a context
        /// </summary>
        /// <param name="contextName">Name of the database context</param>
        /// <param name="entityName">Name of the entity</param>
        /// <returns>Entity definition</returns>
        /// <response code="200">The requested entity definition</response>
        /// <response code="400">If the context or entity name is invalid or missing</response>
        /// <response code="404">If the entity is not found in the context</response>
        /// <response code="500">If an error occurs while retrieving the entity</response>
        [HttpGet("contexts/{contextName}/entities/{entityName}")]
        [ProducesResponseType(typeof(DataStructureDefinitionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetEntity(string contextName, string entityName)
        {
            try
            {
                if (string.IsNullOrEmpty(contextName) || string.IsNullOrEmpty(entityName))
                {
                    logger.LogWarning("Attempted to get entity with invalid parameters. Context: {ContextName}, Entity: {EntityName}", 
                        contextName, entityName);
                    return BadRequest("Both context name and entity name are required");
                }

                logger.LogInformation("Retrieving entity {EntityName} from context {ContextName}", 
                    entityName, contextName);
                var entity = entityCrudService.GetEntity(contextName, entityName);

                if (entity == null)
                {
                    logger.LogWarning("Entity {EntityName} not found in context {ContextName}", 
                        entityName, contextName);
                    return NotFound($"Entity {entityName} not found in context {contextName}");
                }

                logger.LogInformation("Successfully retrieved entity {EntityName} from context {ContextName}", 
                    entityName, contextName);
                return Ok(entity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve entity {EntityName} from context {ContextName}", 
                    entityName, contextName);
                return StatusCode(500, "An error occurred while retrieving the entity");
            }
        }

        /// <summary>
        /// Retrieves records from an entity with pagination support
        /// </summary>
        /// <param name="contextName">Name of the database context</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="paginationParameters">Pagination parameters</param>
        /// <param name="orderBy">Optional ordering expression</param>
        /// <returns>Paged result of entity records</returns>
        /// <response code="200">The requested page of records</response>
        /// <response code="400">If the parameters are invalid</response>
        /// <response code="500">If an error occurs while retrieving records</response>
        [HttpPost("contexts/{contextName}/entities/{entityName}/records")]
        [ProducesResponseType(typeof(DataPagedResult<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetRecords(
            string contextName,
            string entityName,
            [FromBody] DataRequestParametersDto dataRequestParameters,
            [FromQuery] string orderBy = "")
        {
            try
            {
                if (string.IsNullOrEmpty(contextName) || string.IsNullOrEmpty(entityName))
                {
                    logger.LogWarning("Attempted to get records with invalid parameters. Context: {ContextName}, Entity: {EntityName}", 
                        contextName, entityName);
                    return BadRequest("Both context name and entity name are required");
                }

                logger.LogInformation("Retrieving records for entity {EntityName} from context {ContextName} (Page: {PageNumber}, Size: {PageSize})", 
                    entityName, contextName, dataRequestParameters.PageNumber, dataRequestParameters.PageSize);

                var result = await entityCrudService.GetAll(contextName, entityName, dataRequestParameters);
                logger.LogInformation("Successfully retrieved {Count} records for entity {EntityName}", 
                    result.Items.Count(), entityName);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation while retrieving records for entity {EntityName}", entityName);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve records for entity {EntityName} from context {ContextName}", 
                    entityName, contextName);
                return StatusCode(500, "An error occurred while retrieving records");
            }
        }

        /// <summary>
        /// Executes a custom SQL query and returns the results
        /// </summary>
        /// <param name="contextName">Name of the database context</param>
        /// <param name="request">SQL query details and parameters</param>
        /// <returns>Query results</returns>
        /// <response code="200">The query results</response>
        /// <response code="400">If the query or parameters are invalid</response>
        /// <response code="500">If an error occurs while executing the query</response>
        [HttpPost("contexts/{contextName}/query")]
        [ProducesResponseType(typeof(object[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult ExecuteQuery(string contextName, [FromBody] EntityCRUDReadSqlQueryDto request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.SqlQuery))
                {
                    logger.LogWarning("Attempted to execute query with invalid parameters. Context: {ContextName}", contextName);
                    return BadRequest("SQL query is required");
                }

                logger.LogInformation("Executing SQL query in context {ContextName}", contextName);
                var results = entityCrudService.ReadFromSql(contextName, request.SqlQuery, request.Filters);
                logger.LogInformation("Successfully executed SQL query in context {ContextName}", contextName);
                return Ok(results);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute SQL query in context {ContextName}", contextName);
                return StatusCode(500, "An error occurred while executing the SQL query");
            }
        }

        /// <summary>
        /// Creates a new record in the specified entity
        /// </summary>
        /// <param name="contextName">Name of the database context</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="model">The record data</param>
        /// <returns>The created record</returns>
        /// <response code="201">The created record</response>
        /// <response code="400">If the data is invalid</response>
        /// <response code="500">If an error occurs while creating the record</response>
        [HttpPost("contexts/{contextName}/entities/{entityName}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult CreateRecord(string contextName, string entityName, [FromBody] object data)
        {
            try
            {
                if (data == null)
                {
                    logger.LogWarning("Attempted to create record with null data. Context: {ContextName}, Entity: {EntityName}", 
                        contextName, entityName);
                    return BadRequest("Record data is required");
                }

                logger.LogInformation("Creating new record in entity {EntityName} (Context: {ContextName})", 
                    entityName, contextName);
                var result = entityCrudService.Create(contextName, entityName, data);
                logger.LogInformation("Successfully created new record in entity {EntityName}", entityName);
                return CreatedAtAction(nameof(GetRecords), new { contextName, entityName }, result);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation while creating record in entity {EntityName}", entityName);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create record in entity {EntityName} (Context: {ContextName})", 
                    entityName, contextName);
                return StatusCode(500, "An error occurred while creating the record");
            }
        }

        /// <summary>
        /// Updates an existing record in the specified entity
        /// </summary>
        /// <param name="contextName">Name of the database context</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="key">Primary key of the record</param>
        /// <param name="data">The updated record data</param>
        /// <returns>The updated record</returns>
        /// <response code="200">The updated record</response>
        /// <response code="400">If the data is invalid</response>
        /// <response code="404">If the record is not found</response>
        /// <response code="500">If an error occurs while updating the record</response>
        [HttpPut("contexts/{contextName}/entities/{entityName}/{key}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateRecord(string contextName, string entityName, string key, [FromBody] object data)
        {
            try
            {
                if (data == null)
                {
                    logger.LogWarning("Attempted to update record with null data. Context: {ContextName}, Entity: {EntityName}, Key: {Key}", 
                        contextName, entityName, key);
                    return BadRequest("Record data is required");
                }

                logger.LogInformation("Updating record in entity {EntityName} (Context: {ContextName}, Key: {Key})", 
                    entityName, contextName, key);
                var result = entityCrudService.Update(contextName, entityName, data);
                
                if (result == null)
                {
                    logger.LogWarning("Record not found for update. Entity: {EntityName}, Key: {Key}", entityName, key);
                    return NotFound();
                }

                logger.LogInformation("Successfully updated record in entity {EntityName}", entityName);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation while updating record in entity {EntityName}", entityName);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update record in entity {EntityName} (Context: {ContextName}, Key: {Key})", 
                    entityName, contextName, key);
                return StatusCode(500, "An error occurred while updating the record");
            }
        }

        /// <summary>
        /// Deletes a record from the specified entity
        /// </summary>
        /// <param name="contextName">Name of the database context</param>
        /// <param name="entityName">Name of the entity</param>
        /// <param name="key">Primary key of the record to delete</param>
        /// <response code="204">If the record was successfully deleted</response>
        /// <response code="400">If the parameters are invalid</response>
        /// <response code="404">If the record is not found</response>
        /// <response code="500">If an error occurs while deleting the record</response>
        [HttpDelete("contexts/{contextName}/entities/{entityName}/{key}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult DeleteRecord(string contextName, string entityName, string key)
        {
            try
            {
                logger.LogInformation("Deleting record from entity {EntityName} (Context: {ContextName}, Key: {Key})", 
                    entityName, contextName, key);
                
                entityCrudService.Delete(contextName, entityName, key);
                
                logger.LogInformation("Successfully deleted record from entity {EntityName}", entityName);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning(ex, "Invalid operation while deleting record from entity {EntityName}", entityName);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete record from entity {EntityName} (Context: {ContextName}, Key: {Key})", 
                    entityName, contextName, key);
                return StatusCode(500, "An error occurred while deleting the record");
            }
        }

        /// <summary>
        /// Analyzes a SQL query and returns its metadata
        /// </summary>
        /// <param name="contextName">Name of the database context</param>
        /// <param name="request">Query analysis request</param>
        /// <returns>Query metadata and validation results</returns>
        /// <response code="200">The query analysis results</response>
        /// <response code="400">If the query is invalid</response>
        /// <response code="500">If an error occurs during analysis</response>
        [HttpPost("contexts/{contextName}/query/analyze")]
        [ProducesResponseType(typeof(SqlQueryResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeQuery(string contextName, [FromBody] EntityCRUDExecuteSQLQueryDto request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.SqlQuery))
                {
                    logger.LogWarning("Attempted to analyze query with invalid parameters. Context: {ContextName}", contextName);
                    return BadRequest("SQL query is required");
                }

                logger.LogInformation("Analyzing SQL query in context {ContextName}", contextName);
                var result = await entityCrudService.GetSqlQueryEntityDefinition(request);

                if (!result.QuerySuccessful)
                {
                    logger.LogWarning("SQL query analysis failed in context {ContextName}: {Error}", 
                        contextName, result.ErrorMessage);
                    return BadRequest(result.ErrorMessage);
                }

                logger.LogInformation("Successfully analyzed SQL query in context {ContextName}", contextName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze SQL query in context {ContextName}", contextName);
                return StatusCode(500, "An error occurred while analyzing the query");
            }
        }
    }
}
