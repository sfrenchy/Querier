using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities;
using Querier.Api.Application.DTOs;
using Querier.Api.Domain.Common.Models;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing SQL queries
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class SQLQueryController : ControllerBase
    {
        private readonly ISQLQueryService _sqlQueryService;

        public SQLQueryController(ISQLQueryService sqlQueryService)
        {
            _sqlQueryService = sqlQueryService;
        }

        /// <summary>
        /// Get all SQL queries accessible by the current user
        /// </summary>
        /// <returns>List of SQL queries (public ones and those created by the user)</returns>
        /// <response code="200">Returns the list of queries</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SQLQueryDTO>), 200)]
        public async Task<ActionResult<IEnumerable<SQLQueryDTO>>> GetQueries()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var queries = await _sqlQueryService.GetAllQueriesAsync(userId);
            return Ok(queries);
        }

        /// <summary>
        /// Get a specific SQL query by its ID
        /// </summary>
        /// <param name="id">The ID of the SQL query</param>
        /// <returns>The SQL query if found</returns>
        /// <response code="200">Returns the query</response>
        /// <response code="404">If the query is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SQLQuery), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SQLQuery>> GetQuery(int id)
        {
            var query = await _sqlQueryService.GetQueryByIdAsync(id);
            if (query == null) return NotFound();
            return Ok(query);
        }

        /// <summary>
        /// Create a new SQL query
        /// </summary>
        /// <param name="query">The SQL query to create</param>
        /// <returns>The created SQL query</returns>
        /// <response code="201">Returns the newly created query</response>
        /// <response code="400">If the query is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(SQLQuery), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<SQLQuery>> CreateQuery(CreateUpdateSQLQueryDTO createUpdateSqlQueryDto)
        {
            var createdQuery = await _sqlQueryService.CreateQueryAsync(createUpdateSqlQueryDto.Query, createUpdateSqlQueryDto.SampleParameters);
            return CreatedAtAction(nameof(GetQuery), new { id = createdQuery.Id }, createdQuery);
        }

        /// <summary>
        /// Update an existing SQL query
        /// </summary>
        /// <param name="id">The ID of the SQL query to update</param>
        /// <param name="query">The updated SQL query data</param>
        /// <returns>The updated SQL query</returns>
        /// <response code="200">Returns the updated query</response>
        /// <response code="400">If the ID doesn't match the query ID</response>
        /// <response code="404">If the query is not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SQLQuery), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SQLQuery>> UpdateQuery(int id, CreateUpdateSQLQueryDTO createUpdateSqlQueryDto)
        {
            if (id != createUpdateSqlQueryDto.Query.Id) return BadRequest();
            var updatedQuery = await _sqlQueryService.UpdateQueryAsync(createUpdateSqlQueryDto.Query, createUpdateSqlQueryDto.SampleParameters);
            if (updatedQuery == null) return NotFound();
            return Ok(updatedQuery);
        }

        /// <summary>
        /// Delete a SQL query
        /// </summary>
        /// <param name="id">The ID of the SQL query to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the query was successfully deleted</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteQuery(int id)
        {
            await _sqlQueryService.DeleteQueryAsync(id);
            return NoContent();
        }

        /// <summary>
        /// Execute a SQL query with parameters
        /// </summary>
        /// <param name="id">The ID of the SQL query to execute</param>
        /// <param name="parameters">Dictionary of parameters to use in the query</param>
        /// <returns>The query results</returns>
        /// <response code="200">Returns the query results</response>
        /// <response code="404">If the query is not found</response>
        /// <response code="400">If there's an error executing the query</response>
        [HttpPost("{id}/execute")]
        [ProducesResponseType(typeof(PagedResult<dynamic>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<PagedResult<dynamic>>> ExecuteQuery(
            int id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 0,
            [FromBody] Dictionary<string, object>? parameters = null)
        {
            try
            {
                parameters ??= new Dictionary<string, object>();
                var result = await _sqlQueryService.ExecuteQueryAsync(
                    id, 
                    parameters,
                    pageNumber, 
                    pageSize
                );
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
} 