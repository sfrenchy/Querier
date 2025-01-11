using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
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
    /// </remarks>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class SqlQueryController(ISqlQueryService sqlQueryService, ILogger<SqlQueryController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Get all SQL queries accessible by the current user
        /// </summary>
        /// <returns>List of SQL queries (public ones and those created by the user)</returns>
        /// <response code="200">Returns the list of queries</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SQLQueryDTO>), 200)]
        public async Task<ActionResult<IEnumerable<SQLQueryDTO>>> GetQueries()
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
        /// Create a new SQL query
        /// </summary>
        /// <param name="dto">The SQL query to create</param>
        /// <returns>The created SQL query</returns>
        /// <response code="201">Returns the newly created query</response>
        /// <response code="400">If the query is invalid</response>
        [HttpPost]
        [ProducesResponseType(typeof(SQLQuery), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<SQLQuery>> CreateQuery(SQLQueryCreateDto dto)
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
                return CreatedAtAction(nameof(GetQuery), new { id = createdQuery.Id }, createdQuery);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating SQL query");
                return StatusCode(500, "An error occurred while creating the SQL query");
            }
        }

        /// <summary>
        /// Update an existing SQL query
        /// </summary>
        /// <param name="dto">The updated SQL query data</param>
        /// <returns>The updated SQL query</returns>
        /// <response code="200">Returns the updated query</response>
        /// <response code="400">If the ID doesn't match the query ID</response>
        /// <response code="404">If the query is not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SQLQuery), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SQLQuery>> UpdateQuery(SQLQueryUpdateDto dto)
        {
            try
            {
                logger.LogDebug("Updating SQL query with ID {QueryId}", dto?.Query?.Id);
                
                if (dto == null)
                {
                    logger.LogWarning("Invalid request: SQL query data is null");
                    return BadRequest("SQL query data is required");
                }

                var updatedQuery = await sqlQueryService.UpdateQueryAsync(dto.Query, dto.SampleParameters);
                
                if (updatedQuery == null)
                {
                    logger.LogWarning("SQL query with ID {QueryId} not found", dto.Query?.Id);
                    return NotFound();
                }

                logger.LogInformation("Successfully updated SQL query with ID {QueryId}", updatedQuery.Id);
                return Ok(updatedQuery);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating SQL query with ID {QueryId}", dto?.Query?.Id);
                return StatusCode(500, "An error occurred while updating the SQL query");
            }
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
        /// Execute a SQL query with parameters
        /// </summary>
        /// <param name="id">The ID of the SQL query to execute</param>
        /// <param name="pageSize">Number of item in a page</param>
        /// <param name="parameters">Dictionary of parameters to use in the query</param>
        /// <param name="pageNumber">The requested page number</param>
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
            [FromBody] Dictionary<string, object> parameters = null)
        {
            try
            {
                logger.LogDebug("Executing SQL query with ID {QueryId}, Page {PageNumber}, Size {PageSize}", 
                    id, pageNumber, pageSize);

                parameters ??= new Dictionary<string, object>();
                logger.LogDebug("Query parameters: {@Parameters}", parameters);

                var result = await sqlQueryService.ExecuteQueryAsync(
                    id, 
                    parameters,
                    pageNumber, 
                    pageSize
                );

                logger.LogInformation(
                    "Successfully executed SQL query with ID {QueryId}. Results retrieved on page {PageNumber}", 
                    id, pageNumber);
                
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