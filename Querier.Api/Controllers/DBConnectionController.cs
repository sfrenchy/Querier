using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Querier.Api.Application.DTOs.Requests.DBConnection;
using Querier.Api.Domain.Entities.QDBConnection;
using Querier.Api.Domain.Services;
using System.Collections.Generic;
using System;
using System.Linq;
using Querier.Api.Application.DTOs.Responses.DBConnection;

namespace Querier.Api.Controllers
{
    /// <summary>
    /// Controller for managing database connections
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for:
    /// - Adding new database connections
    /// - Reading existing database connections
    /// - Deleting database connections
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
    /// - 401 Unauthorized: Authentication required
    /// - 403 Forbidden: User lacks required permissions
    /// - 500 Internal Server Error: Unexpected server error
    /// </remarks>
    [Authorize]
    [Route("api/v1/[controller]")]
    [ApiController]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public class DBConnectionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IDBConnectionService _dbConnectionService;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<DBConnectionController> _logger;

        public DBConnectionController(IHostApplicationLifetime hostApplicationLifetime, IDBConnectionService dbConnectionService, IConfiguration configuration, ILogger<DBConnectionController> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _dbConnectionService = dbConnectionService;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        /// <summary>
        /// Adds a new database connection
        /// </summary>
        /// <remarks>
        /// Creates a new database connection with the provided configuration.
        /// 
        /// Sample request:
        ///     POST /api/v1/dbconnection/adddbconnection
        ///     {
        ///         "name": "MyDatabase",
        ///         "connectionString": "Server=myserver;Database=mydb;User Id=myuser;Password=mypassword;"
        ///     }
        /// </remarks>
        /// <param name="connection">The database connection configuration</param>
        /// <returns>The created database connection details</returns>
        /// <response code="200">Returns the created database connection</response>
        /// <response code="400">If the connection configuration is invalid</response>
        [HttpPost("AddDbConnection")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddDBConnectionAsync([FromBody] AddDBConnectionRequest connection)
        {
            return Ok(await _dbConnectionService.AddConnectionAsync(connection));
        }

        /// <summary>
        /// Deletes a database connection
        /// </summary>
        /// <remarks>
        /// Removes a database connection from the system.
        /// 
        /// Sample request:
        ///     DELETE /api/v1/dbconnection/deletedbconnection
        ///     {
        ///         "id": "123"
        ///     }
        /// </remarks>
        /// <param name="request">The identifier of the connection to delete</param>
        /// <returns>The result of the deletion operation</returns>
        /// <response code="200">Connection was successfully deleted</response>
        /// <response code="404">If the connection was not found</response>
        [HttpDelete("DeleteDBConnection")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDBConnectionAsync(DeleteDBConnectionRequest request)
        {
            return Ok(await _dbConnectionService.DeleteDBConnectionAsync(request));
        }

        /// <summary>
        /// Gets all database connections
        /// </summary>
        /// <returns>List of database connections</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _dbConnectionService.GetAll());
        }

        /// <summary>
        /// Gets the database schema description
        /// </summary>
        /// <remarks>
        /// Returns a detailed description of the database schema including:
        /// - Tables with their columns and relationships
        /// - Views with their columns
        /// - Stored procedures with their parameters
        /// - User-defined functions
        /// 
        /// Sample request:
        ///     GET /api/v1/dbconnection/{connectionId}/schema
        /// </remarks>
        /// <param name="connectionId">The ID of the database connection</param>
        /// <returns>Detailed description of the database schema</returns>
        /// <response code="200">Returns the database schema description</response>
        /// <response code="404">If the connection was not found</response>
        /// <response code="400">If there's an error retrieving the schema</response>
        [HttpGet("{connectionId}/schema")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDatabaseSchema(int connectionId)
        {
            try
            {
                var schema = await _dbConnectionService.GetDatabaseSchema(connectionId);
                return Ok(schema);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Database connection with ID {connectionId} not found");
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database schema for connection {ConnectionId}", connectionId);
                return StatusCode(500, "An error occurred while retrieving the database schema");
            }
        }

        /// <summary>
        /// Analyzes a SQL query to find referenced objects
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/v1/dbconnection/{connectionId}/analyze-query
        ///     {
        ///         "query": "SELECT o.OrderID, o.OrderDate FROM Orders o",
        ///         "parameters": {}
        ///     }
        /// </remarks>
        /// <param name="connectionId">Database connection ID</param>
        /// <param name="request">Query to analyze</param>
        /// <response code="200">Returns the list of referenced objects</response>
        /// <response code="400">If the query is invalid</response>
        /// <response code="404">If the connection was not found</response>
        [HttpPost("{connectionId}/analyze-query")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<string>>> AnalyzeQuery(
            int connectionId, 
            [FromBody] AnalyzeQueryRequest request)
        {
            try
            {
                var objects = await _dbConnectionService.GetQueryObjects(connectionId, request.Query);
                return Ok(objects);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Database connection with ID {connectionId} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing query");
                return StatusCode(500, "An error occurred while analyzing the query");
            }
        }

        /// <summary>
        /// Enumerates database servers available on the network
        /// </summary>
        /// <remarks>
        /// Searches for accessible database servers of the specified type on the network.
        /// 
        /// Supported database types:
        /// - SQLServer
        /// - MySQL
        /// - PostgreSQL
        /// 
        /// Sample request:
        ///     GET /api/v1/dbconnection/enumerate-servers/SQLServer
        /// </remarks>
        /// <param name="databaseType">Type of database to search for</param>
        /// <returns>List of found servers with their information</returns>
        /// <response code="200">Returns the list of found servers</response>
        /// <response code="400">If the database type is not supported</response>
        [HttpGet("enumerate-servers/{databaseType}")]
        [ProducesResponseType(typeof(List<DatabaseServerInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<DatabaseServerInfo>>> EnumerateServers(string databaseType)
        {
            try
            {
                if (!new[] { "SQLServer", "MySQL", "PostgreSQL" }.Contains(databaseType))
                {
                    return BadRequest($"Database type '{databaseType}' is not supported. Supported types are: SQLServer, MySQL, PostgreSQL");
                }

                var servers = await _dbConnectionService.EnumerateServersAsync(databaseType);
                return Ok(servers);
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating {DatabaseType} servers", databaseType);
                return StatusCode(500, $"An error occurred while enumerating {databaseType} servers");
            }
        }
    }
}
