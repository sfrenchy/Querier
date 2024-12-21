using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Querier.Api.Domain.Entities;
using Querier.Api.Domain.Exceptions;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing SQL queries
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
        [ProducesResponseType(typeof(IEnumerable<SQLQuery>), 200)]
        public async Task<ActionResult<IEnumerable<SQLQuery>>> GetQueries()
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
        public async Task<ActionResult<SQLQuery>> CreateQuery(SQLQuery query)
        {
            var createdQuery = await _sqlQueryService.CreateQueryAsync(query);
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
        public async Task<ActionResult<SQLQuery>> UpdateQuery(int id, SQLQuery query)
        {
            if (id != query.Id) return BadRequest();
            var updatedQuery = await _sqlQueryService.UpdateQueryAsync(query);
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
        [ProducesResponseType(typeof(IEnumerable<dynamic>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<dynamic>>> ExecuteQuery(int id, [FromBody] Dictionary<string, object> parameters)
        {
            try
            {
                var results = await _sqlQueryService.ExecuteQueryAsync(id, parameters);
                return Ok(results);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 