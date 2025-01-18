using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Domain.Common.Models;
using Querier.Api.Domain.Entities;
using Querier.Api.Domain.Exceptions;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for executing and managing SQL queries
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Executing SQL queries
    /// - Managing query parameters
    /// - Analyzing query performance
    /// - Handling query results
    /// 
    /// ## Authentication
    /// All endpoints in this controller require authentication.
    /// Use a valid JWT token in the Authorization header:
    /// ```
    /// Authorization: Bearer {your-jwt-token}
    /// ```
    /// 
    /// ## Common Responses
    /// - 200 OK: Operation completed successfully
    /// - 201 Created: Resource created successfully
    /// - 400 Bad Request: Invalid input data
    /// - 401 Unauthorized: Authentication required
    /// - 403 Forbidden: User lacks required permissions
    /// - 404 Not Found: Resource not found
    /// - 500 Internal Server Error: Unexpected server error
    /// </remarks>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class SqlQueryController(ISqlQueryService sqlQueryService, ILogger<SqlQueryController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves all SQL queries accessible by the current user
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/sqlquery
        /// </remarks>
        /// <returns>List of SQL queries (public ones and those created by the user)</returns>
        /// <response code="200">Returns the list of queries</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SqlQueryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SqlQueryDto>>> GetAllAsync()
        {
            try
            {
                logger.LogDebug("Getting all SQL queries for current user");
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    logger.LogWarning("User ID not found in claims");
                    return Unauthorized("User ID not found in claims");
                }

                var queries = await sqlQueryService.GetAllQueriesAsync(userId);
                logger.LogInformation("Successfully retrieved {Count} queries for user {UserId}", queries.ToList().Count, userId);
                return Ok(queries);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving SQL queries");
                return StatusCode(500, "An error occurred while retrieving SQL queries");
            }
        }

        /// <summary>
        /// Retrieves a specific SQL query by its ID
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     GET /api/v1/sqlquery/123
        /// </remarks>
        /// <param name="id">The ID of the SQL query</param>
        /// <returns>The SQL query if found</returns>
        /// <response code="200">Returns the requested query</response>
        /// <response code="404">If the query was not found</response>
        [HttpGet("{id}", Name = "GetSqlQueryById")]
        [ProducesResponseType(typeof(SQLQuery), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SQLQuery>> GetByIdAsync(int id)
        {
            try
            {
                logger.LogDebug("Getting SQL query with ID: {QueryId}", id);
                var query = await sqlQueryService.GetQueryByIdAsync(id);
                
                if (query == null)
                {
                    logger.LogWarning("SQL query with ID {QueryId} not found", id);
                    return NotFound();
                }

                logger.LogInformation("Successfully retrieved SQL query with ID {QueryId}", id);
                return Ok(query);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving SQL query with ID {QueryId}", id);
                return StatusCode(500, "An error occurred while retrieving the SQL query");
            }
        }

        /// <summary>
        /// Creates a new SQL query
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/sqlquery
        ///     {
        ///         "query": "SELECT * FROM Users WHERE Age > @age",
        ///         "sampleParameters": {
        ///             "age": 18
        ///         }
        ///     }
        /// </remarks>
        /// <param name="dto">The SQL query to create</param>
        /// <returns>The created SQL query</returns>
        /// <response code="201">Returns the newly created query</response>
        /// <response code="400">If the query is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(SQLQuery), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SQLQuery>> CreateAsync(SQLQueryCreateDto dto)
        {
            try
            {
                logger.LogDebug("Creating new SQL query");
                
                if (dto == null)
                {
                    logger.LogWarning("Invalid request: SQL query data is null");
                    return BadRequest("SQL query data is required");
                }

                var createdQuery = await sqlQueryService.CreateQueryAsync(dto.Query, dto.SampleParameters);
                logger.LogInformation("Successfully created SQL query with ID {QueryId}", createdQuery.Id);
                return CreatedAtRoute("GetSqlQueryById", new { id = createdQuery.Id }, createdQuery);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating SQL query");
                return StatusCode(500, "An error occurred while creating the SQL query");
            }
        }

        /// <summary>
        /// Updates an existing SQL query
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     PUT /api/v1/sqlquery/123
        ///     {
        ///         "query": {
        ///             "id": 123,
        ///             "sql": "SELECT * FROM Users WHERE Age > @age"
        ///         },
        ///         "sampleParameters": {
        ///             "age": 21
        ///         }
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the query to update</param>
        /// <param name="dto">The updated SQL query data</param>
        /// <returns>The updated SQL query</returns>
        /// <response code="200">Returns the updated query</response>
        /// <response code="400">If the request data is invalid</response>
        /// <response code="404">If the query was not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SQLQuery), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SQLQuery>> UpdateAsync(int id, SQLQueryUpdateDto dto)
        {
            try
            {
                logger.LogDebug("Updating SQL query with ID {QueryId}", id);
                
                if (dto == null)
                {
                    logger.LogWarning("Invalid request: SQL query data is null");
                    return BadRequest("SQL query data is required");
                }

                if (dto.Query?.Id != id)
                {
                    return BadRequest("ID mismatch between URL and body");
                }

                var updatedQuery = await sqlQueryService.UpdateQueryAsync(dto.Query, dto.SampleParameters);
                
                if (updatedQuery == null)
                {
                    logger.LogWarning("SQL query with ID {QueryId} not found", id);
                    return NotFound($"SQL query with ID {id} not found");
                }

                logger.LogInformation("Successfully updated SQL query with ID {QueryId}", updatedQuery.Id);
                return Ok(updatedQuery);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating SQL query with ID {QueryId}", id);
                return StatusCode(500, "An error occurred while updating the SQL query");
            }
        }

        /// <summary>
        /// Deletes a SQL query
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     DELETE /api/v1/sqlquery/123
        /// </remarks>
        /// <param name="id">The ID of the SQL query to delete</param>
        /// <returns>No content if successful</returns>
        /// <response code="204">If the query was successfully deleted</response>
        /// <response code="404">If the query was not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                logger.LogDebug("Deleting SQL query with ID {QueryId}", id);
                await sqlQueryService.DeleteQueryAsync(id);
                logger.LogInformation("Successfully deleted SQL query with ID {QueryId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deleting SQL query with ID {QueryId}", id);
                return StatusCode(500, "An error occurred while deleting the SQL query");
            }
        }

        /// <summary>
        /// Executes a SQL query with parameters
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///     POST /api/v1/sqlquery/123/execute?pageNumber=1&amp;pageSize=10
        ///     {
        ///         "age": 18,
        ///         "status": "active"
        ///     }
        /// </remarks>
        /// <param name="id">The ID of the SQL query to execute</param>
        /// <param name="pageNumber">The requested page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 0 for all)</param>
        /// <param name="parameters">Dictionary of parameters to use in the query</param>
        /// <returns>The query results</returns>
        /// <response code="200">Returns the query results</response>
        /// <response code="400">If there's an error executing the query</response>
        /// <response code="404">If the query was not found</response>
        [HttpPost("{id}/execute")]
        [ProducesResponseType(typeof(DataPagedResult<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DataPagedResult<dynamic>>> ExecuteAsync(
            int id,
            [FromBody] DataRequestParametersWtihSQLParametersDto parameters)
        {
            try
            {
                logger.LogDebug("Executing SQL query with ID {QueryId}, Page {PageNumber}, Size {PageSize}", 
                    id, parameters.PageNumber, parameters.PageSize);

                logger.LogDebug("Query parameters: {@Parameters}", parameters);

                var result = await sqlQueryService.ExecuteQueryAsync(
                    id, 
                    parameters
                );

                logger.LogInformation(
                    "Successfully executed SQL query with ID {QueryId}. Results retrieved on page {PageNumber}", 
                    id, parameters.PageNumber);
                
                return Ok(result);
            }
            catch (NotFoundException ex)
            {
                logger.LogWarning(ex, "SQL query with ID {QueryId} not found", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing SQL query with ID {QueryId}", id);
                return BadRequest(ex.Message);
            }
        }
    }
} 