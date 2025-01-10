using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.DTOs.Requests.Entity;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Services;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for handling CRUD operations on entities
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Creating, reading, updating, and deleting entities
    /// - Executing SQL queries on entities
    /// - Managing entity data filters
    /// - Handling entity-specific operations
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EntityCRUDController : ControllerBase
    {
        private readonly IEntityCRUDService _entityCRUDService;
        private readonly ILogger<EntityCRUDController> _logger;

        public EntityCRUDController(IEntityCRUDService entityCRUDService, ILogger<EntityCRUDController> logger)
        {
            _logger = logger;
            _entityCRUDService = entityCRUDService;
        }

        /// <summary>
        /// Gets all available database contexts
        /// </summary>
        /// <returns>List of available database contexts</returns>
        /// <response code="200">Returns the list of contexts</response>
        [HttpGet("GetContexts")]
        public async Task<IActionResult> GetContexts()
        {
            return new OkObjectResult(await _entityCRUDService.GetContextsAsync());
        }

        /// <summary>
        /// Gets all entities for a specific context
        /// </summary>
        /// <param name="contextTypeName">The name of the context type</param>
        /// <returns>List of entities in the context</returns>
        /// <response code="200">Returns the list of entities</response>
        [HttpGet("GetEntities")]
        public IActionResult GetEntities(string contextTypeName)
        {
            return new OkObjectResult(_entityCRUDService.GetEntities(contextTypeName));
        }

        /// <summary>
        /// Gets a specific entity from a context
        /// </summary>
        /// <param name="contextTypeName">The name of the context type</param>
        /// <param name="entityName">The name of the entity</param>
        /// <returns>The requested entity</returns>
        /// <response code="200">Returns the entity information</response>
        [HttpGet("GetEntity")]
        public IActionResult GetEntity(string contextTypeName, string entityName)
        {
            return new OkObjectResult(_entityCRUDService.GetEntity(contextTypeName, entityName));
        }

        /// <summary>
        /// Gets all records for a specific entity with pagination
        /// </summary>
        /// <param name="contextTypeName">The name of the context type</param>
        /// <param name="entityTypeName">The name of the entity type</param>
        /// <param name="pageNumber">Page number for pagination</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="OrderBy">Property name to order by</param>
        /// <returns>Paginated list of entity records</returns>
        /// <response code="200">Returns the paginated list of records</response>
        [HttpGet("GetAll")]
        public IActionResult GetAll(
            [FromQuery] string contextTypeName, 
            [FromQuery] string entityTypeName,
            [FromQuery] int pageNumber = 0,
            [FromQuery] int pageSize = 0,
            [FromQuery] string OrderBy = "")
        {
            var paginationParams = new PaginationParameters 
            { 
                PageNumber = pageNumber,
                PageSize = pageSize 
            };
            
            var result = _entityCRUDService.GetAll(contextTypeName, entityTypeName, paginationParams, OrderBy);
            return Ok(result);
        }

        /// <summary>
        /// Executes a custom SQL query on an entity
        /// </summary>
        /// <param name="request">The SQL query request containing context, query, and filters</param>
        /// <returns>Query results</returns>
        /// <response code="200">Returns the query results</response>
        [HttpPost("ReadFromSql")]
        public IActionResult ReadFromSql([FromBody] EntityCRUDReadSqlQueryDto request)
        {
            var datas = _entityCRUDService.ReadFromSql(request.ContextTypeName, request.SqlQuery, request.Filters);
            return new OkObjectResult(datas);
        }

        /// <summary>
        /// Creates a new entity record
        /// </summary>
        /// <param name="model">The entity data to create</param>
        /// <returns>The created entity record</returns>
        /// <response code="200">Returns the created record</response>
        [HttpPost("Create")]
        public IActionResult Create([FromBody] Application.DTOs.EntityCRUDCreateOrUpdateDto model)
        {
            return new OkObjectResult(_entityCRUDService.Create(model.ContextTypeName, model.EntityType, model.Data));
        }

        /// <summary>
        /// Updates an existing entity record
        /// </summary>
        /// <param name="model">The updated entity data</param>
        /// <returns>The updated entity record</returns>
        /// <response code="200">Returns the updated record</response>
        [HttpPost("Update")]
        public IActionResult Update([FromBody] Application.DTOs.EntityCRUDCreateOrUpdateDto model)
        {
            return new OkObjectResult(_entityCRUDService.Update(model.ContextTypeName, model.EntityType, model.Data));
        }

        /// <summary>
        /// Creates or updates an entity record
        /// </summary>
        /// <param name="model">The entity data to create or update</param>
        /// <returns>The created or updated entity record</returns>
        /// <response code="200">Returns the created or updated record</response>
        [HttpPost("CreateOrUpdate")]
        public IActionResult CreateOrUpdate([FromBody] Application.DTOs.EntityCRUDCreateOrUpdateDto model)
        {
            return new OkObjectResult(_entityCRUDService.CreateOrUpdate(model.ContextTypeName, model.EntityType, model.Data));
        }

        /// <summary>
        /// Deletes an entity record
        /// </summary>
        /// <param name="model">The entity record to delete</param>
        /// <returns>Success indicator</returns>
        /// <response code="200">If the deletion was successful</response>
        [HttpDelete("Delete")]
        public IActionResult Delete([FromBody] EntityCRUDDeleteDto model)
        {
            _entityCRUDService.Delete(model.ContextTypeName, model.EntityType, model.Key);
            return new OkResult();
        }

        /// <summary>
        /// Gets the entity definition for a SQL query
        /// </summary>
        /// <param name="request">The SQL query request</param>
        /// <returns>Entity definition based on the query</returns>
        /// <response code="200">Returns the entity definition</response>
        [HttpPost("GetSQLQueryEntityDefinition")]
        public IActionResult GetSQLQueryEntityDefinition([FromBody] EntityCRUDExecuteSQLQueryDto request)
        {
            var res = _entityCRUDService.GetSQLQueryEntityDefinition(request);
            return new OkObjectResult(res);
        }
    }
}
